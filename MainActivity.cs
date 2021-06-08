using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using Android.Graphics;
using Android.Widget;
using Plugin.Media;
using System;
using System.IO;
using Plugin.CurrentActivity;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Android.Runtime;

namespace AccesoAS
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        string Archivo;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            
            SupportActionBar.Hide();
            CrossCurrentActivity.Current.Init(this, savedInstanceState);
            var Imagen = FindViewById<ImageView>(Resource.Id.imagen);
            var btnAlmacenar = FindViewById<Button>(Resource.Id.btnalmacenar);
            var txtNombre = FindViewById<EditText>(Resource.Id.txtnombre);
            var txtDomicilio = FindViewById<EditText>(Resource.Id.txtdomicilio);
            var txtEdad = FindViewById<EditText>(Resource.Id.txtedad);
            var txtCorreo = FindViewById<EditText>(Resource.Id.txtcorreo);
            var txtSaldo = FindViewById<EditText>(Resource.Id.txtsaldo);
            Imagen.Click += async delegate
            {
                await CrossMedia.Current.Initialize();
                var archivo = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
                {
                    Directory = "Imagenes",
                    Name = txtNombre.Text,
                    SaveToAlbum = true,
                    CompressionQuality = 30,
                    CustomPhotoSize = 30,
                    PhotoSize = Plugin.Media.Abstractions.PhotoSize.Medium,
                    DefaultCamera = Plugin.Media.Abstractions.CameraDevice.Rear
                });
                if (archivo == null)
                    return;
                Bitmap bp = BitmapFactory.DecodeStream(archivo.GetStream());
                Archivo = System.IO.Path.Combine(System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                    txtNombre.Text + ".jpg"));
                var stream = new FileStream(Archivo, FileMode.Create);
                bp.Compress(Bitmap.CompressFormat.Png, 30, stream);
                stream.Close();
                Imagen.SetImageBitmap(bp);
                long memoria1 = GC.GetTotalMemory(false);
                Toast.MakeText(this.ApplicationContext, memoria1.ToString(),
                    ToastLength.Long).Show();
                long memoria2 = GC.GetTotalMemory(false);
                Toast.MakeText(this.ApplicationContext, memoria2.ToString(),
                    ToastLength.Long).Show();

            };

            btnAlmacenar.Click += async delegate
            {
                try
                {
                    var CuentadeAlmacenamiento = CloudStorageAccount.Parse
                    ("DefaultEndpointsProtocol=https;AccountName=programacionmoviles;AccountKey=K4HLGMkMGB87LlncsykIQe5QO85Ges6DDZ1wjK8M7EFpZeR+k+7fKLm3uy3th+R6mvmYeDa6pf2sn62Q3dZkWg==;EndpointSuffix=core.windows.net");
                    var ClienteBlob = CuentadeAlmacenamiento.CreateCloudBlobClient();
                    var Carpeta = ClienteBlob.GetContainerReference("enrique");
                    var resourceBlob = Carpeta.GetBlockBlobReference(txtNombre.Text + ".jpg");
                    await resourceBlob.UploadFromFileAsync(Archivo.ToString());
                    Toast.MakeText(this, "Imagen almacenada en el contenedor de blobs en Azure", ToastLength.Long).Show();

                    var TablaNoQSL = CuentadeAlmacenamiento.CreateCloudTableClient();
                    var Coleccion = TablaNoQSL.GetTableReference("Registro");
                    await Coleccion.CreateIfNotExistsAsync();
                    var clientes = new Clientes("Clientes", txtNombre.Text);
                    clientes.Correo = txtCorreo.Text;
                    clientes.Saldo = double.Parse(txtSaldo.Text);
                    clientes.Edad = int.Parse(txtEdad.Text);
                    clientes.Domicilio = txtDomicilio.Text;
                    clientes.ImagenBlob = txtNombre.Text + ".jpg";
                    var Store = TableOperation.Insert(clientes);
                    await Coleccion.ExecuteAsync(Store);
                    Toast.MakeText(this, "Datos guardados en Tabla NoSQL en Azure", ToastLength.Long).Show();

                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message ,ToastLength.Long).Show();

                }
            };
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
    public class Clientes : TableEntity
    {
        public Clientes (string Categoria, string Nombre)
        {
            PartitionKey = Categoria;
            RowKey = Nombre;
        }
        public string Correo { get; set; }
        public string Domicilio { get; set; }
        public int Edad { get; set; }
        public double Saldo { get; set; }
        public string ImagenBlob { get; set; }
    }
}