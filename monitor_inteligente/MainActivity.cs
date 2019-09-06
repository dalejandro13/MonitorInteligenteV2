using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System.Threading.Tasks;
using System.IO;
using System;
using Android.Media;
using Android.Views;
using Android.Net;
using Plugin.Connectivity;
using System.Linq;
using Plugin.Connectivity.Abstractions;
using Android.Net.Wifi;
using System.Net;
using System.ComponentModel;
using System.Threading;
using System.Collections.Generic;
using Android.Content;
using Android.Support.V4.Content;
using Android;
using Android.App.Admin;
using static Android.OS.PowerManager;
using Java.Net;
using System.Net.Sockets;
using static Android.App.ActivityManager;
using System.Timers;

namespace monitor_inteligente
{
    //190.70.19.54
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        string path_arch = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).ToString() + "/archivos";
        string ruta1 = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString() + "/archivos/parametros.txt";
        string path_archivos = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString() + "/archivos";
        int ctrl = 0, hora_actual = 0, hi_aux = 0, hf_aux = 0, ctrl_lectura = 0, ctrl_lea = 0, conteo_lineas = 0, min_aux = 0, desconect = 0, level = 0, rompe = 0;
        bool isSaving = false, ctrlWifi = true, hour_enabled = true, playing = false, readagain = false, trydownload = false, offline = true, cicleactive = false;
        public double bytesIn = 0, percentage = 0;
        string id, nm, hi, hf, ca, mi, nom1, dia, mes, año, fecha_actual, hora, min, aux_nm, aux_name;
        public string linea1;
        long free_size = 0, totalbytesfiles = 0, bytesfile = 0, bytesfileserver = 0, blocksize = 0, blockavailables = 0;
        public List<string> lista = new List<string>();
        public int recorre = 0;
        ProgressDialog progreso;
        public System.Timers.Timer WakeupScreen;
        private VideoView video_main;
        DateTime time, date;
        public VideoView Video_main { get => video_main; set => video_main = value; }
        public WebClient cliente = new WebClient();
        public WifiManager wifi;
        public PowerManager pm;
        public WakeLock wakeLock;
        public WakeLock wake;
        public Intent intent;
        public Intent IntentService;
        WifiInfo wifiinfo;
        //Task t;

        public async Task CleanMemory()
        {
            recorre = 0;
            rompe = 0;
            ctrl_lectura = 0;
            ctrl_lea = 0;
            ctrl = 0;
            aux_nm = null;
            aux_name = null;
            dia = null;
            mes = null;
            año = null;
            fecha_actual = null;
            hour_enabled = true;
            playing = false;
            id = null;
            nm = null;
            hi = null;
            hf = null;
            ca = null;
            linea1 = null;
            lista.Clear();
        }

        ///////////////////////////////////////////////////////////
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            SupportActionBar.SetDisplayShowTitleEnabled(false); //quita el titulo de action bar
            SupportActionBar.Hide(); //quita el action bar
            InitializeTimer(); //timer
            Video_main = FindViewById<VideoView>(Resource.Id.video_main);
            progreso = new ProgressDialog(this);
            progreso.SetTitle("Descargando archivos...");
            progreso.SetCancelable(true);
            progreso.SetProgressStyle(ProgressDialogStyle.Horizontal);
            progreso.SetCanceledOnTouchOutside(false);
            progreso.Max = 100;
            wifi = (WifiManager)GetSystemService(Android.Content.Context.WifiService); //obtiene los servicios de wifi
            await Folder();

            if (wifi.IsWifiEnabled)
            {
                while (ctrlWifi)
                {
                    Thread.Sleep(5000);
                    wifiinfo = wifi.ConnectionInfo;
                    level = WifiManager.CalculateSignalLevel(wifiinfo.Rssi, 11);
                    if (level >= 3)
                    {
                        cicleactive = true;
                        await Download();
                        if (CrossConnectivity.Current.IsConnected) //verifica nuevamente si hay conexion con internet 
                        {
                            if (!WakeupScreen.Enabled)
                            {
                                WakeupScreen.Start();
                            }
                            WakeupScreen.Start();
                            intent = PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
                            StartActivity(intent);
                            //pm = (PowerManager)GetSystemService(Context.PowerService);
                            //wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "stay awake gently");
                            //wakeLock.Acquire();
                            //StartService(new Intent(this, typeof(BackgroundService))); //AGREGADO
                        }
                        else
                        {
                            if (WakeupScreen.Enabled)
                            {
                                WakeupScreen.Stop();
                            }
                            cicleactive = false;
                            pm = (PowerManager)GetSystemService(Context.PowerService);
                            wakeLock = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                            wakeLock.Acquire();
                            wakeLock.Release();
                            //StopService(new Intent(this, typeof(BackgroundService))); //AGREGADO
                            await ReadFile(); //AGREGADO
                        }
                    }
                    else
                    {
                        ctrlWifi = false;
                        await ReadFile(); //AGREGADO
                    }
                }
            }
            else
            {
                await ReadFile();
            }

            CrossConnectivity.Current.ConnectivityChanged += async delegate //detecta eventos de conexion por wifi
            {
                //t.Dispose();
                Thread.Sleep(5000);
                offline = true;
                if (CrossConnectivity.Current.IsConnected.ToString().Equals("false"))
                {
                    if (CrossConnectivity.Current.ConnectionTypes.Contains(ConnectionType.WiFi))
                    {
                        //si se pierde la conexion pero el wifi todavia funciona
                        Thread.Sleep(5000);
                        wifiinfo = wifi.ConnectionInfo;
                        level = WifiManager.CalculateSignalLevel(wifiinfo.Rssi, 11); //calcula la potencia de la señal wifi
                        if (level < 3)
                        {
                            if (WakeupScreen.Enabled)
                            {
                                WakeupScreen.Stop();
                            }
                            cicleactive = false;
                            offline = false;
                            level = 0;
                            Toast.MakeText(this, "conexion perdida con wifi", ToastLength.Long).Show();
                            ctrlWifi = false;
                            pm = (PowerManager)GetSystemService(Context.PowerService);
                            wakeLock = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                            wakeLock.Acquire();
                            wakeLock.Release();
                            //StopService(new Intent(this, typeof(BackgroundService))); //AGREGADO
                            await ReadFile(); //AGREGADO
                        }
                        //pm = (PowerManager)GetSystemService(Context.PowerService);
                        //wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                        //wake.Acquire();
                    }
                }
                else
                {
                    if (CrossConnectivity.Current.ConnectionTypes.Contains(ConnectionType.WiFi)) //wifi esta encendido y esta conectado
                    {
                        ctrlWifi = true;
                        while (ctrlWifi)
                        {
                            Thread.Sleep(5000);
                            wifiinfo = wifi.ConnectionInfo;
                            level = WifiManager.CalculateSignalLevel(wifiinfo.Rssi, 11); //calcula la potencia de la señal wifi
                            if (level >= 3)
                            {
                                if (!WakeupScreen.Enabled)
                                {
                                    WakeupScreen.Start();
                                }
                                cicleactive = true;
                                offline = true;
                                await Download(); //cuando hay conexion a internet por wifi, inicia proceso para la descarga de archivos
                                intent = PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
                                StartActivity(intent); //el dispositivo entra a hibernar
                                //StartService(new Intent(this, typeof(BackgroundService))); //AGREGADO
                            }
                            else
                            {
                                if (WakeupScreen.Enabled)
                                {
                                    WakeupScreen.Stop();
                                }
                                cicleactive = false;
                                offline = false;
                                ctrlWifi = false;
                                pm = (PowerManager)GetSystemService(Context.PowerService);
                                wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                                wake.Acquire(); //el dispositivo sale de hibernar
                                wake.Release();
                                //StopService(new Intent(this, typeof(BackgroundService))); //AGREGADO
                                await ReadFile(); //AGREGADO
                            }
                        }
                    }
                    else //cuando no hay conexion a internet
                    {
                        Thread.Sleep(3000);
                        wifiinfo = wifi.ConnectionInfo;
                        level = WifiManager.CalculateSignalLevel(wifiinfo.Rssi, 11); //calcula la potencia de la señal wifi
                        if (level < 3)
                        {
                            if (WakeupScreen.Enabled)
                            {
                                WakeupScreen.Stop();
                            }
                            cicleactive = false;
                            offline = false;
                            level = 0;
                            Toast.MakeText(this, "sin conexion por wifi", ToastLength.Long).Show();
                            pm = (PowerManager)GetSystemService(Context.PowerService);
                            wakeLock = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                            wakeLock.Acquire();
                            wakeLock.Release();
                            //StopService(new Intent(this, typeof(BackgroundService))); //AGREGADO
                            await ReadFile(); //AGREGADO
                        }
                        //pm = (PowerManager)GetSystemService(Context.PowerService);
                        //wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                        //wake.Acquire();
                    }
                }
            };
        }

        private void InitializeTimer()
        {
            WakeupScreen = new System.Timers.Timer();
            WakeupScreen.Interval = 60000;//900000; //15 min
            WakeupScreen.Enabled = true;
            WakeupScreen.Elapsed += OnOff;
        }

        private void OnOff(object sender, ElapsedEventArgs e)
        {
            pm = (PowerManager)GetSystemService(Context.PowerService);
            wakeLock = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
            wakeLock.Acquire();
            wakeLock.Release();
            Thread.Sleep(2000);
            intent = PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
            StartActivity(intent); //el dispositivo entra a hibernar

            //StopService(new Intent(this, typeof(BackgroundService))); //AGREGADO
        }

        //private async void OnOff(object state)
        //{
        //    pm = (PowerManager)GetSystemService(Context.PowerService);
        //    wifiinfo = wifi.ConnectionInfo;
        //    level = WifiManager.CalculateSignalLevel(wifiinfo.Rssi, 11);
        //    if (level >= 3)
        //    {
        //        wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
        //        wake.Acquire();
        //        wake.Release();
        //        intent = PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
        //        StartActivity(intent);
        //        wake = pm.NewWakeLock(WakeLockFlags.Partial, "stay awake gently");
        //        wake.Acquire();
        //    }
        //    else
        //    {
        //        wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
        //        wake.Acquire();
        //        wake.Release();
        //    }
        //}

        async Task Download()
        {
            //primero verificar espacio disponible en memoria
            free_size = 0;
            totalbytesfiles = 0;
            blocksize = 0;
            blockavailables = 0;
            StatFs stat = new StatFs(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString() + "/archivos");
            blocksize = stat.BlockSize;
            blockavailables = stat.AvailableBlocks;
            free_size = blocksize * blockavailables; //tamaño libre de memoria en tv Box

            wifiinfo = wifi.ConnectionInfo;
            level = WifiManager.CalculateSignalLevel(wifiinfo.Rssi, 11); //calcula la potencia de la señal wifi

            if (level >= 3)
            {
                level = 0;
                try
                {
                    //verifica que si la memoria del tvBox esta llena
                    using (cliente.OpenRead(new System.Uri("https://flexolumens.co/PantallaInterna/parametros.txt")))
                    {
                        //bytesfileserver
                        bytesfileserver = Convert.ToInt64(cliente.ResponseHeaders["Content-Length"]); //mido el tamaño del archivo que esta en el servido                        
                    }
                    if (free_size <= bytesfileserver)
                    {
                        bytesfileserver = 0;
                        Toast.MakeText(this, "no hay espacio suficiente en la memoria", ToastLength.Long).Show();
                        var list = Directory.GetFiles(path_archivos, "*.mp4"); //rutina para borrar todo el contenido en el interior de la carpeta, mas no borrar la propia carpeta
                        if (list.Length > 0)
                        {
                            for (int i = 0; i < list.Length; i++)
                            {
                                File.Delete(list[i]);
                            }
                        }
                    }

                    if (!File.Exists(ruta1))
                    {
                        File.WriteAllText(ruta1, string.Empty);
                    }
                    //FileInfo f1 = new FileInfo(ruta1);
                    //if (f1.Length != tam_parametros)
                    //{
                    using (WebClient cliente1 = new WebClient()) //descarga el archivo que contiene los parametros
                    {
                        try
                        {
                            readagain = true;
                            cliente1.DownloadFileCompleted += new AsyncCompletedEventHandler(completado);
                            cliente1.DownloadProgressChanged += new DownloadProgressChangedEventHandler(cargando);
                            await cliente1.DownloadFileTaskAsync(new System.Uri("https://flexolumens.co/PantallaInterna/parametros.txt"), ruta1);
                            //await Task.Delay(3000);
                            Thread.Sleep(3000);
                            readagain = true;
                        }
                        catch (WebException exe)
                        {

                        }
                    }
                    //}
                    //Leer archivo en tvBox para identificar el Bus
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                }
                try
                {
                    using (var lines = new StreamReader(path_archivos + "/parametros.txt"))
                    {
                        string line = string.Empty;
                        while ((line = lines.ReadLine()) != null) //se lee linea por linea el archivo parametros.txt y lo guarda en la variable line
                        {
                            if (line[0] == '<' && line[1] == 'n' && line[2] == 'm')
                            {
                                isSaving = true;
                                for (int i = 4; i < line.Length; i++)
                                {
                                    if (isSaving == true)
                                    {
                                        if (line[i] == '<')
                                        {
                                            try
                                            {
                                                isSaving = false;
                                                readagain = true;
                                                var pathserver = "https://flexolumens.co/PantallaInterna/" + nm;
                                                using (cliente.OpenRead(new System.Uri(pathserver))) //comparar tamaño del archivo del servidor con el que tiene internamente en la memoria
                                                {
                                                    bytesfile = Convert.ToInt64(cliente.ResponseHeaders["Content-Length"]); //mido el tamaño del archivo que esta en el servidor
                                                    totalbytesfiles = totalbytesfiles + bytesfile;
                                                    nm = string.Empty;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                //no hagas nada
                                            }
                                        }
                                        else
                                        {
                                            nm += line[i];
                                        }
                                    }
                                }
                            }
                        }
                        if (free_size <= totalbytesfiles)
                        {
                            totalbytesfiles = 0;
                            Toast.MakeText(this, "no hay espacio suficiente en la memoria", ToastLength.Long).Show();
                            var list = Directory.GetFiles(path_archivos, "*.mp4"); //rutina para borrar todo el contenido en el interior de la carpeta, mas no borrar la propia carpeta
                            if (list.Length > 0)
                            {
                                for (int i = 0; i < list.Length; i++)
                                {
                                    File.Delete(list[i]);
                                }
                            }
                        }
                        else
                        {
                            using (var lines1 = new StreamReader(path_archivos + "/parametros.txt")) //verificar cada archivo para evitar que se descarguen nuevamente los mismos videos
                            {
                                nm = string.Empty;
                                line = string.Empty;
                                while ((line = lines1.ReadLine()) != null) //se lee linea por linea del archivo parametros.txt
                                {
                                    if (line[0] == '<' && line[1] == 'n' && line[2] == 'm')
                                    {
                                        isSaving = true;
                                        for (int i = 4; i < line.Length; i++)
                                        {
                                            if (isSaving == true)
                                            {
                                                if (line[i] == '<')
                                                {
                                                    if (CrossConnectivity.Current.IsConnected)
                                                    {
                                                        isSaving = false;
                                                        readagain = true;
                                                        var pathserver = "https://flexolumens.co/PantallaInterna/" + nm;
                                                        var pathvideo = path_archivos + "/" + nm;

                                                        if (File.Exists(pathvideo))
                                                        {
                                                            using (cliente.OpenRead(new System.Uri(pathserver)))
                                                            {
                                                                bytesfileserver = Convert.ToInt64(cliente.ResponseHeaders["Content-Length"]); //mido el tamaño del archivo que esta en el servidor
                                                            }
                                                            FileInfo fi = new FileInfo(pathvideo);
                                                            long info = fi.Length; //longitud en bytes del video
                                                            if (info == bytesfileserver) //son de tamaño igual, no descargues el video
                                                            {
                                                                nm = string.Empty;
                                                                bytesfileserver = 0;
                                                            }
                                                            else 
                                                            {
                                                                //PROCESO DE DESCARGA DE ARCHIVOS
                                                                cliente.DownloadFileCompleted += new AsyncCompletedEventHandler(completado);
                                                                cliente.DownloadProgressChanged += new DownloadProgressChangedEventHandler(cargando);
                                                                progreso.SetMessage("Descargando " + nm);
                                                                progreso.Show();
                                                                await cliente.DownloadFileTaskAsync(new System.Uri(pathserver), pathvideo);
                                                                //await Task.Delay(3000);
                                                                Thread.Sleep(3000);
                                                                progreso.Dismiss();
                                                            }
                                                        }
                                                        else
                                                        {
                                                            try
                                                            {
                                                                using (cliente.OpenRead(new System.Uri(pathserver)))
                                                                {
                                                                    bytesfileserver = Convert.ToInt64(cliente.ResponseHeaders["Content-Length"]); //mido el tamaño del archivo que esta en el servidor para poder descargarlo
                                                                }
                                                                try
                                                                {
                                                                    //inicia descarga del video en el servidor 
                                                                    cliente.DownloadProgressChanged += new DownloadProgressChangedEventHandler(cargando);
                                                                    cliente.DownloadFileCompleted += new AsyncCompletedEventHandler(completado);
                                                                    progreso.SetMessage("Descargando " + nm);
                                                                    progreso.Show();
                                                                    await cliente.DownloadFileTaskAsync(new System.Uri(pathserver), pathvideo);
                                                                    //await Task.Delay(3000);
                                                                    Thread.Sleep(3000);
                                                                    progreso.Dismiss();
                                                                }
                                                                catch (WebException Ex)
                                                                {
                                                                    if (CrossConnectivity.Current.IsConnected) //intenta de nuevo descargar el archivo
                                                                    {
                                                                        if (nm == "") //si por alguna razon alcanza la reconexion pero nm se borra, retoma de nuevo el valor con aux_name
                                                                        {
                                                                            nm = aux_name;
                                                                        }
                                                                        pathserver = "https://flexolumens.co/PantallaInterna/" + nm;
                                                                        using (cliente.OpenRead(new System.Uri(pathserver)))
                                                                        {
                                                                            bytesfileserver = Convert.ToInt64(cliente.ResponseHeaders["Content-Length"]); //mido el tamaño del archivo que esta en el servidor
                                                                        }
                                                                        cliente.DownloadProgressChanged += new DownloadProgressChangedEventHandler(cargando);
                                                                        cliente.DownloadFileCompleted += new AsyncCompletedEventHandler(completado);
                                                                        progreso.SetMessage("Descargando " + nm);
                                                                        progreso.Show();
                                                                        await cliente.DownloadFileTaskAsync(new System.Uri(pathserver), pathvideo);
                                                                        //await Task.Delay(3000);
                                                                        Thread.Sleep(3000);
                                                                        trydownload = false;
                                                                        progreso.Dismiss();
                                                                    }
                                                                    else
                                                                    {
                                                                        Toast.MakeText(this, "descarga de archivos interrumpida", ToastLength.Short).Show();
                                                                    }
                                                                }
                                                            }
                                                            catch (WebException Ex)
                                                            {
                                                                //no hagas nada
                                                            }
                                                        }
                                                        ctrlWifi = false; //para salir del ciclo while
                                                    }
                                                    else
                                                    {
                                                        nm = string.Empty;
                                                        isSaving = false;
                                                    }
                                                }
                                                else
                                                {
                                                    nm += line[i];
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    Toast.MakeText(this, "error de lectura", ToastLength.Long).Show();
                }
            }
            else
            {
                blocksize = 0;
                blockavailables = 0;
                free_size = 0;
                Thread.Sleep(3000);
                desconect++;
            }
            if (desconect > 3)
            {
                desconect = 0;
                ctrlWifi = false;
            }

            var newUiOptions = (int)Window.DecorView.SystemUiVisibility;
            newUiOptions |= (int)SystemUiFlags.LowProfile;
            newUiOptions |= (int)SystemUiFlags.Fullscreen;
            newUiOptions |= (int)SystemUiFlags.HideNavigation;
            newUiOptions |= (int)SystemUiFlags.ImmersiveSticky;
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
        }

        private void completado(object sender, AsyncCompletedEventArgs e)
        {
            bytesIn = 0;
            percentage = 0;
            bytesfileserver = 0;
            if (!CrossConnectivity.Current.IsConnected)
            {
                Toast.MakeText(this, "descarga de archivos interrumpida", ToastLength.Short).Show();
                progreso.Dismiss();
                aux_name = nm;
                bool reconnecting = true;
                while (reconnecting)
                {
                    if (!CrossConnectivity.Current.IsConnected)
                    {
                        rompe++;
                        Thread.Sleep(10000);
                    }
                    else if (CrossConnectivity.Current.IsConnected)
                    {
                        rompe = 0;
                        reconnecting = false;
                        trydownload = true;
                    }
                    if (rompe >= 12) //esperar por 2 min si logra alcanzar reconexion por wifi
                    {
                        rompe = 0;
                        reconnecting = false;
                        File.Delete(Path.Combine(path_archivos, nm)); //borrar el archivo que quedo con descarga inconclusa (Corrupto)
                        nm = string.Empty;
                        pm = (PowerManager)GetSystemService(Context.PowerService);
                        wakeLock = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                        wakeLock.Acquire();
                    }
                }
            }
            else
            {
                nm = string.Empty;
            }
        }

        private void cargando(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                bytesIn = double.Parse(e.BytesReceived.ToString());
                percentage = bytesIn / bytesfileserver * 100;
                progreso.Progress = int.Parse(Math.Truncate(percentage).ToString());
            }
            catch (Exception)
            {
                //Toast.MakeText(this, "error en la descarga", ToastLength.Long).Show();
            }
        }

        async Task Folder() //se crea carpeta de archivos si no existe
        {
            try
            {
                var folders_files = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString() + "/archivos"; // /storage/emulated/0/Movies/Main
                if (!Directory.Exists(folders_files)) //si el directorio no existe
                {
                    Directory.CreateDirectory(folders_files); //entonces crea una nueva
                }
            }
            catch (Exception)
            {
                Toast.MakeText(this, "error", ToastLength.Long);
            }
        }

        async Task ReadFile()
        {
            var newUiOptions = (int)Window.DecorView.SystemUiVisibility;
            newUiOptions |= (int)SystemUiFlags.LowProfile;
            newUiOptions |= (int)SystemUiFlags.Fullscreen;
            newUiOptions |= (int)SystemUiFlags.HideNavigation;
            newUiOptions |= (int)SystemUiFlags.ImmersiveSticky;
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;

            await stayalert();
            try
            {
                rompe = 0;
                date = DateTime.Today;
                dia = date.Day.ToString();
                mes = date.Month.ToString();
                año = date.Year.ToString();
                fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual
                hour_enabled = true;
                playing = false;

                DirectoryInfo dataDir = new DirectoryInfo(path_archivos);
                FileInfo[] v = dataDir.GetFiles("*.mp4");
                FileInfo[] f = dataDir.GetFiles("*.txt");
                var tam = v.Length;
                var tam1 = f.Length;
                if (tam != 0 && tam1 != 0)
                {
                    nom1 = f[0].Name; //nombre del archivo txt
                    string rut_archivo = Path.Combine(path_archivos, nom1); //ruta del archivo de los parametros
                    string line = string.Empty; //se declara variable que almacene linea por linea
                    ctrl_lectura = 0;
                    ctrl_lea = 0;
                    ctrl = 0;
                    id = string.Empty;
                    nm = string.Empty;
                    hi = string.Empty;
                    hf = string.Empty;
                    ca = string.Empty;

                    using (var lines = new StreamReader(rut_archivo))
                    {
                        conteo_lineas = File.ReadAllLines(rut_archivo).Length; //obtiene el numero de lineas disponibles en el archivo
                        while ((line = lines.ReadLine()) != null) //se lee linea por linea del archivo parametros.txt y lo guarda en line
                        {
                            if (line[0] == '<' && line[1] == 'i' && line[2] == 'd' && line[3] == '>')
                            {
                                ctrl = 0;
                                ctrl_lea++;
                                isSaving = true;
                                ctrl++;
                                id = string.Empty;
                                for (int i = 4; i < line.Length; i++)
                                {
                                    if (isSaving == true)
                                    {
                                        if (line[i] == '<')
                                        {
                                            isSaving = false;
                                        }
                                        else
                                        {
                                            id += line[i];
                                        }
                                    }
                                }
                            }
                            else if (line[0] == '<' && line[1] == 'n' && line[2] == 'm' && line[3] == '>')
                            {
                                ctrl_lea++;
                                isSaving = true;
                                ctrl++;
                                nm = string.Empty;
                                for (int i = 4; i < line.Length; i++)
                                {
                                    if (isSaving == true)
                                    {
                                        if (line[i] == '<')
                                        {
                                            isSaving = false;
                                        }
                                        else
                                        {
                                            nm += line[i];
                                        }
                                    }
                                }
                            }
                            else if (line[0] == '<' && line[1] == 'h' && line[2] == 'i' && line[3] == '>')
                            {
                                ctrl_lea++;
                                isSaving = true;
                                ctrl++;
                                hi = string.Empty;
                                for (int i = 4; i < line.Length; i++)
                                {
                                    if (isSaving == true)
                                    {
                                        if (line[i] == '<')
                                        {
                                            isSaving = false;
                                        }
                                        else
                                        {
                                            hi += line[i];
                                        }
                                    }
                                }
                            }
                            else if (line[0] == '<' && line[1] == 'h' && line[2] == 'f' && line[3] == '>')
                            {
                                ctrl_lea++;
                                isSaving = true;
                                ctrl++;
                                hf = string.Empty;
                                for (int i = 4; i < line.Length; i++)
                                {
                                    if (isSaving == true)
                                    {
                                        if (line[i] == '<')
                                        {
                                            isSaving = false;
                                        }
                                        else
                                        {
                                            hf += line[i];
                                        }
                                    }
                                }
                            }
                            else if (line[0] == '<' && line[1] == 'c' && line[2] == 'a' && line[3] == '>')
                            {
                                ctrl_lea++;
                                isSaving = true;
                                ctrl++;
                                ca = string.Empty;
                                for (int i = 4; i < line.Length; i++)
                                {
                                    if (isSaving == true)
                                    {
                                        if (line[i] == '<')
                                        {
                                            isSaving = false;
                                        }
                                        else
                                        {
                                            ca += line[i];
                                        }
                                    }
                                }
                            }
                            else //si aparece la etiqueta <mi>
                            {
                                id = string.Empty;
                                nm = string.Empty;
                                hi = string.Empty;
                                hf = string.Empty;
                                ca = string.Empty;
                                ctrl_lea++;
                                ctrl = 0;
                            }


                            if (ctrl >= 5)
                            {
                                ctrl = 0;
                                time = DateTime.Now.ToLocalTime();
                                hora = time.Hour.ToString();
                                min = time.Minute.ToString();
                                hora_actual = Convert.ToInt32(hora);
                                string hora_actual1 = hora + ":" + min;

                                hi_aux = Convert.ToInt32(hi);
                                hf_aux = Convert.ToInt32(hf);
                                min_aux = Convert.ToInt32(min);

                                if (ca == fecha_actual && (hora_actual >= hi_aux && hora_actual < hf_aux)) // si el archivo tiene la fecha actual y la hora actual esta dentro del rango que muestra ese archivo, entonces reproduce el video
                                {
                                    lista.Clear();
                                    id = string.Empty;
                                    hi = string.Empty;
                                    hf = string.Empty;
                                    ca = string.Empty;
                                    try
                                    {
                                        string rut_video = Path.Combine(path_archivos, nm); //toma ruta del video
                                        if (File.Exists(rut_video))
                                        {
                                            if (!Video_main.IsPlaying)
                                            {
                                                aux_nm = nm;
                                                ctrl_lea = 0; //variable que incremen
                                                hour_enabled = true;
                                                VideoPlay(video_main, rut_video);
                                            }
                                        }
                                        else
                                        {
                                            Toast.MakeText(this, "no hay videos para reproducir", ToastLength.Long).Show();
                                            rut_video = Path.Combine(path_archivos, aux_nm); //toma ruta del video
                                        }
                                        nm = string.Empty;
                                    }
                                    catch (Exception ex)
                                    {
                                        //no hagas nada
                                    }
                                }
                                else
                                {
                                    id = string.Empty;
                                    nm = string.Empty;
                                    hi = string.Empty;
                                    hf = string.Empty;
                                    ca = string.Empty;
                                }
                            }
                        }
                    }
                    if (conteo_lineas == ctrl_lea)
                    {
                        conteo_lineas = 0;
                        ctrl_lea = 0;
                        hour_enabled = false;
                        //await ReadFile(); //LINEA NUEVA
                        //poner alguna rutina que reproduzca algun video cuando en el archivo no se encuentra alguno con la fecha de hoy
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////////////
                    /////////inicia de nuevo la lectura del archivo para que lea las etiquetas de los minutos//////////
                    if (hour_enabled == false)
                    {
                        ctrl = 0;

                        id = string.Empty;
                        nm = string.Empty;
                        mi = string.Empty;
                        ca = string.Empty;
                        //obtiene los nombres de los videos para almacenarlos en una lista
                        if (lista.Count == 0)
                        {
                            using (var lines = new StreamReader(rut_archivo))
                            {
                                while ((line = lines.ReadLine()) != null) //se lee linea por linea del archivo parametros.txt y lo guarda en line
                                {
                                    if (line[0] == '<' && line[1] == 'n' && line[2] == 'm' && line[3] == '>')
                                    {
                                        isSaving = true;
                                        nm = string.Empty;
                                        for (int i = 4; i < line.Length; i++)
                                        {
                                            if (isSaving == true)
                                            {
                                                if (line[i] == '<')
                                                {
                                                    isSaving = false;
                                                }
                                                else
                                                {
                                                    nm += line[i];
                                                }
                                            }
                                        }
                                    }
                                    else if (line[0] == '<' && line[1] == 'm' && line[2] == 'i' && line[3] == '>')
                                    {
                                        isSaving = true;
                                        mi = string.Empty;
                                        for (int i = 4; i < line.Length; i++)
                                        {
                                            if (isSaving == true)
                                            {
                                                if (line[i] == '<')
                                                {
                                                    isSaving = false;
                                                }
                                                else
                                                {
                                                    mi += line[i];
                                                }
                                            }
                                        }
                                    }
                                    else if (line[0] == '<' && line[1] == 'c' && line[2] == 'a' && line[3] == '>')
                                    {
                                        isSaving = true;
                                        ca = string.Empty;
                                        for (int i = 4; i < line.Length; i++)
                                        {
                                            if (isSaving == true)
                                            {
                                                if (line[i] == '<')
                                                {
                                                    isSaving = false;
                                                }
                                                else
                                                {
                                                    ca += line[i];
                                                }
                                            }
                                        }
                                        if (/*ca == fecha_actual &&*/ mi != string.Empty)
                                        {
                                            lista.Add(nm);
                                            mi = string.Empty;
                                            nm = string.Empty;
                                            ca = string.Empty;
                                        }
                                    }
                                    else
                                    {
                                        mi = string.Empty;
                                        nm = string.Empty;
                                        ca = string.Empty;
                                    }
                                }
                            }
                        }
                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        if (lista.Count != 0)
                        {
                            ctrl = 0;
                            id = string.Empty;
                            nm = string.Empty;
                            mi = string.Empty;
                            ca = string.Empty;
                            try
                            {
                                await selectVideo();
                                nm = string.Empty;
                            }
                            catch (Exception ex)
                            {
                                //no hagas nada
                            }
                        }
                        else //cuando no se encuentre un video con la fecha actual
                        {
                            using (var lines = new StreamReader(ruta1))
                            {
                                while ((line = lines.ReadLine()) != null) //se lee linea por linea del archivo parametros.txt y lo guarda en line
                                {
                                    if (line[0] == '<' && line[1] == 'n' && line[2] == 'm' && line[3] == '>')
                                    {
                                        isSaving = true;
                                        nm = string.Empty;
                                        for (int i = 4; i < line.Length; i++)
                                        {
                                            if (isSaving == true)
                                            {
                                                if (line[i] == '<')
                                                {
                                                    isSaving = false;
                                                }
                                                else
                                                {
                                                    nm += line[i];
                                                }
                                            }
                                        }
                                    }
                                    else if (line[0] == '<' && line[1] == 'm' && line[2] == 'i' && line[3] == '>')
                                    {
                                        lista.Add(nm);
                                        mi = string.Empty;
                                        nm = string.Empty;
                                    }
                                    else
                                    {
                                        mi = string.Empty;
                                        nm = string.Empty;
                                    }
                                }
                            }
                            mi = string.Empty;
                            try
                            {
                                await selectVideo();
                                nm = string.Empty;
                            }
                            catch (Exception ex)
                            {
                                //no hagas nada
                            }
                        }
                    }
                    ////////////////////////////////////////////////////////////////////////////////////////////////
                }
            }
            catch (Exception)
            {
                //no hagas nada
            }
        }

        async Task stayalert()
        {
            try
            {
                using (WebClient pet = new WebClient())
                {
                    if (CrossConnectivity.Current.IsConnected)
                    {
                        HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create("http://www.google.com");
                        myRequest.Timeout = 10000;
                        HttpWebResponse response = (HttpWebResponse)myRequest.GetResponse();

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            response.Close();
                        }
                        else
                        {
                            response.Close();
                        }

                        pm = (PowerManager)GetSystemService(Context.PowerService);
                        //wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "stay awake gently");
                        //wakeLock.Acquire();
                        if (CrossConnectivity.Current.IsConnected)
                        {
                            intent = PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
                            StartActivity(intent);
                            wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "stay awake gently");
                            wakeLock.Acquire();
                        }
                        else
                        {
                            pm = (PowerManager)GetSystemService(Context.PowerService);
                            wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                            wake.Acquire();
                            wake.Release();
                        }
                    }
                    else
                    {
                        pm = (PowerManager)GetSystemService(Context.PowerService);
                        wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                        wake.Acquire();
                        wake.Release();
                    }
                }
            }
            catch (Exception Ex)
            {
                wifiinfo = wifi.ConnectionInfo;
                level = WifiManager.CalculateSignalLevel(wifiinfo.Rssi, 11); //calcula la potencia de la señal wifi
                if (level >= 3)
                {
                    intent = PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
                    StartActivity(intent);
                }
                else
                {
                    pm = (PowerManager)GetSystemService(Context.PowerService);
                    wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                    wake.Acquire();
                }
            }
        }

        async Task selectVideo()
        {
            while (recorre < lista.Count)
            {
                aux_nm = lista[recorre];
                string rut_video = Path.Combine(path_archivos, aux_nm); //toma ruta del video
                if (File.Exists(rut_video))
                {
                    if (playing == false)
                    {
                        playing = true;
                        hour_enabled = false;
                        recorre++;
                        VideoPlay(video_main, rut_video);
                        break;
                    }
                }
                else
                {
                    recorre++;
                }
            }
            if (lista.Count == 1)
            {
                recorre = 0;
            }
            else if (recorre == lista.Count)
            {
                recorre = 0;
                lista.Clear(); //reinicia la lista 
                await ReadFile(); //comienza otra vez con la lectura del archivo
            }
        }

        async Task VideoPlay(VideoView video_main, string ruta)
        {
            try
            {
                video_main.SetOnPreparedListener(new VideoLoop());
                video_main.SetOnCompletionListener(new VideoLoop1(video_main, lista, recorre, intent, this.ApplicationContext));
                if (!video_main.IsPlaying)
                {
                    try
                    {
                        video_main.SetVideoPath(ruta);
                        video_main.RequestFocus(); 
                        video_main.Start();
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, "no se puede reproducir video", ToastLength.Long).Show();
                    }
                }
            }
            catch (Exception ex)
            {
                //no hagas nada
            }
        }

        public class VideoLoop1 : Java.Lang.Object, MediaPlayer.IOnCompletionListener
        {
            string ruta2 = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString() + "/archivos/parametros.txt";
            string path_archivos = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString() + "/archivos";
            DateTime time, date;
            int ctrl = 0, hora_actual = 0, hi_aux = 0, hf_aux = 0, ctrl_lectura = 0, ctrl_lea = 0, conteo_lineas = 0, min_aux = 0, recorre = 1, level = 0;
            bool isSaving = false, ciclo = true, hour_enabled = true, playing = false;
            public string id, nm, hi, hf, ca, mi, nom, nom1, dia, mes, año, fecha_actual, hora, min, aux_nm, rutVideodata;
            long tam_parametros = 0;
            VideoView localVideoView;
            List<string> lista2;
            WebClient cliente;
            public PowerManager pm;
            public WakeLock wake;
            public WakeLock wakeLock;
            public Intent inten;
            public Context context;
            public WifiManager wifi;
            public WifiInfo wifiinfo;

            //public async Task CleanMemory()
            //{
            //    recorre = 0;
            //    //level = -1;
            //    ctrl_lectura = 0;
            //    ctrl_lea = 0;
            //    ctrl = 0;
            //    //pm = null;
            //    //wake = null;
            //    //wakeLock = null;
            //    aux_nm = null;
            //    dia = null;
            //    mes = null;
            //    año = null;
            //    fecha_actual = null;
            //    hour_enabled = true;
            //    playing = false;
            //    id = null;
            //    nm = null;
            //    hi = null;
            //    hf = null;
            //    ca = null;
            //    //wifiinfo = null;
            //    //inten = null;
            //    lista2.Clear();
            //}

            public VideoLoop1(VideoView videoView, List<string> videos, int counter, Intent intent, Context originContext)
            {
                localVideoView = videoView; //puente entre videoview de la clase mainactivity y videoloop1
                lista2 = videos;
                recorre = counter;
                inten = intent;
                context = originContext;
            }

            public async void OnCompletion(MediaPlayer mp)
            {
                try
                {
                    mp.Stop();
                    if (!CrossConnectivity.Current.IsConnected) //solo reproduce si no hay señal wifi
                    {
                        if (recorre == lista2.Count && hour_enabled == false)
                        {
                            recorre = 0;
                            lista2.Clear(); //reinicia nuevamente a lista2
                            await ReadFile(); //comienza otra vez con la lectura del archivo
                        }
                        else
                        {
                            await ReadFile();
                        }
                    }
                }
                catch (Exception ex)
                {
                    //no hagas nada
                }
            }

            async Task ReadFile()
            {
                await stayalert();
                try
                {
                    date = DateTime.Today;
                    dia = date.Day.ToString();
                    mes = date.Month.ToString();
                    año = date.Year.ToString();
                    fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual
                    hour_enabled = true;
                    playing = false;
                    hi = string.Empty;
                    hf = string.Empty;
                    ctrl = 0;
                    DirectoryInfo dataDir = new DirectoryInfo(path_archivos);
                    FileInfo[] v = dataDir.GetFiles("*.mp4");
                    FileInfo[] f = dataDir.GetFiles("*.txt");
                    var tam = v.Length;
                    var tam1 = f.Length;
                    if (tam != 0 && tam1 != 0)
                    {
                        nom1 = f[0].Name; //nombre del archivo txt
                        string rut_archivo = Path.Combine(path_archivos, nom1); //ruta del archivo de los parametros
                        string line = string.Empty; //se declara variable que almacene linea por linea
                        ctrl_lectura = 0;
                        ctrl_lea = 0;
                        ctrl = 0;
                        id = string.Empty;
                        nm = string.Empty;
                        hi = string.Empty;
                        hf = string.Empty;
                        ca = string.Empty;
                        using (var lines = new StreamReader(rut_archivo))
                        {
                            conteo_lineas = File.ReadAllLines(rut_archivo).Length; //obtiene el numero de lineas disponibles en el archivo
                            while ((line = lines.ReadLine()) != null) //se lee linea por linea del archivo parametros.txt y lo guarda en line
                            {
                                if (line[0] == '<' && line[1] == 'i' && line[2] == 'd' && line[3] == '>')
                                {
                                    ctrl_lea++;
                                    ctrl++;
                                    isSaving = true;
                                    id = string.Empty;
                                    for (int i = 4; i < line.Length; i++)
                                    {
                                        if (isSaving == true)
                                        {
                                            if (line[i] == '<')
                                            {
                                                isSaving = false;
                                            }
                                            else
                                            {
                                                id += line[i];
                                            }
                                        }
                                    }
                                }
                                else if (line[0] == '<' && line[1] == 'n' && line[2] == 'm' && line[3] == '>')
                                {
                                    ctrl_lea++;
                                    ctrl++;
                                    isSaving = true;
                                    nm = string.Empty;
                                    for (int i = 4; i < line.Length; i++)
                                    {
                                        if (isSaving == true)
                                        {
                                            if (line[i] == '<')
                                            {
                                                isSaving = false;
                                            }
                                            else
                                            {
                                                nm += line[i];
                                            }
                                        }
                                    }
                                }
                                else if (line[0] == '<' && line[1] == 'h' && line[2] == 'i' && line[3] == '>')
                                {
                                    ctrl_lea++;
                                    ctrl++;
                                    isSaving = true;
                                    hi = string.Empty;
                                    for (int i = 4; i < line.Length; i++)
                                    {
                                        if (isSaving == true)
                                        {
                                            if (line[i] == '<')
                                            {
                                                isSaving = false;
                                            }
                                            else
                                            {
                                                hi += line[i];
                                                Thread.Sleep(100);
                                            }
                                        }
                                    }
                                }
                                else if (line[0] == '<' && line[1] == 'h' && line[2] == 'f' && line[3] == '>')
                                {
                                    ctrl_lea++;
                                    ctrl++;
                                    isSaving = true;
                                    hf = string.Empty;
                                    for (int i = 4; i < line.Length; i++)
                                    {
                                        if (isSaving == true)
                                        {
                                            if (line[i] == '<')
                                            {
                                                isSaving = false;
                                            }
                                            else
                                            {
                                                hf += line[i];
                                                Thread.Sleep(100);
                                            }
                                        }
                                    }
                                }
                                else if (line[0] == '<' && line[1] == 'c' && line[2] == 'a' && line[3] == '>')
                                {
                                    ctrl_lea++;
                                    ctrl++;
                                    isSaving = true;
                                    ca = string.Empty;
                                    for (int i = 4; i < line.Length; i++)
                                    {
                                        if (isSaving == true)
                                        {
                                            if (line[i] == '<')
                                            {
                                                isSaving = false;
                                            }
                                            else
                                            {
                                                ca += line[i];
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    id = string.Empty;
                                    nm = string.Empty;
                                    hi = string.Empty;
                                    hf = string.Empty;
                                    ca = string.Empty;
                                    ctrl_lea++;
                                    ctrl = 0;
                                }

                                if (ctrl >= 5)
                                {
                                    ctrl = 0;
                                    time = DateTime.Now.ToLocalTime();
                                    hora = time.Hour.ToString();
                                    min = time.Minute.ToString();
                                    hora_actual = Convert.ToInt32(hora);
                                    string hora_actual1 = hora + ":" + min;
                                    hi_aux = Convert.ToInt32(hi);
                                    hf_aux = Convert.ToInt32(hf);
                                    min_aux = Convert.ToInt32(min);

                                    if (ca == fecha_actual && (hora_actual >= hi_aux && hora_actual < hf_aux)) // si el archivo tiene la fecha actual y la hora actual esta dentro del rango que muestra ese archivo, entonces reproduce el video
                                    {
                                        lista2.Clear(); //aca
                                        id = string.Empty;
                                        hi = string.Empty;
                                        hf = string.Empty;
                                        ca = string.Empty;
                                        ctrl_lea = 0;
                                        try
                                        {
                                            string rut_video = Path.Combine(path_archivos, nm); //toma ruta del video
                                            if (File.Exists(rut_video))
                                            {
                                                if (!localVideoView.IsPlaying)
                                                {
                                                    aux_nm = nm;
                                                    ctrl_lea = 0; //variable que incremen
                                                    hour_enabled = true;
                                                    VideoPlay(localVideoView, rut_video);
                                                }
                                            }
                                            else
                                            {
                                                Toast.MakeText(Application.Context, "no hay videos para rerpoducir", ToastLength.Long).Show();
                                                //Toast.MakeText(Application.Context, "reproduciendo otro video", ToastLength.Long).Show();
                                                //rut_video = Path.Combine(path_archivos, aux_nm); //toma ruta del video
                                            }
                                            nm = string.Empty;
                                        }
                                        catch (Exception ex)
                                        {
                                            //no hagas nada
                                        }
                                    }
                                    else
                                    {
                                        id = string.Empty;
                                        nm = string.Empty;
                                        hi = string.Empty;
                                        hf = string.Empty;
                                        ca = string.Empty;
                                        ctrl = 0;
                                    }
                                }
                            }
                        }
                        if (conteo_lineas == ctrl_lea)
                        {
                            conteo_lineas = 0;
                            ctrl_lea = 0;
                            hour_enabled = false; //si no encuentra reproduccion por horas
                            //await ReadFile(); //LINEA NUEVA
                            //poner alguna rutina que reproduzca algun video cuando en el archivo no se encuentra alguno con la fecha de hoy
                        }

                        if (hour_enabled == false)
                        {
                            ctrl = 0;
                            id = string.Empty;
                            nm = string.Empty;
                            mi = string.Empty;
                            ca = string.Empty;
                            /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                            //obtiene los nombres de los videos para almacenarlos en una lista
                            ///////////////////// si no encuentra videos por horas empieza a buscar por minutos ///////////////
                            if (lista2.Count == 0)
                            {
                                using (var lines = new StreamReader(rut_archivo))
                                {
                                    while ((line = lines.ReadLine()) != null) //se lee linea por linea del archivo parametros.txt y lo guarda en line
                                    {
                                        if (line[0] == '<' && line[1] == 'n' && line[2] == 'm' && line[3] == '>')
                                        {
                                            isSaving = true;
                                            nm = string.Empty;
                                            for (int i = 4; i < line.Length; i++)
                                            {
                                                if (isSaving == true)
                                                {
                                                    if (line[i] == '<')
                                                    {
                                                        isSaving = false;
                                                    }
                                                    else
                                                    {
                                                        nm += line[i];
                                                    }
                                                }
                                            }
                                        }
                                        else if (line[0] == '<' && line[1] == 'm' && line[2] == 'i' && line[3] == '>')
                                        {
                                            isSaving = true;
                                            mi = string.Empty;
                                            for (int i = 4; i < line.Length; i++)
                                            {
                                                if (isSaving == true)
                                                {
                                                    if (line[i] == '<')
                                                    {
                                                        isSaving = false;
                                                    }
                                                    else
                                                    {
                                                        mi += line[i];
                                                    }
                                                }
                                            }
                                        }
                                        else if (line[0] == '<' && line[1] == 'c' && line[2] == 'a' && line[3] == '>')
                                        {
                                            isSaving = true;
                                            ca = string.Empty;
                                            for (int i = 4; i < line.Length; i++)
                                            {
                                                if (isSaving == true)
                                                {
                                                    if (line[i] == '<')
                                                    {
                                                        isSaving = false;
                                                    }
                                                    else
                                                    {
                                                        ca += line[i];
                                                    }
                                                }
                                            }
                                            if (/*ca == fecha_actual &&*/ mi != string.Empty)
                                            {
                                                lista2.Add(nm);
                                                mi = string.Empty;
                                                nm = string.Empty;
                                                ca = string.Empty;
                                            }
                                        }
                                        else
                                        {
                                            mi = string.Empty;
                                            nm = string.Empty;
                                            ca = string.Empty;
                                        }
                                    }
                                }
                            }
                            ///////////////////////////////////////////////////////////////
                            if (lista2.Count != 0)
                            {
                                if (lista2.Count == 1)
                                {
                                    recorre = 0;
                                }
                                ctrl = 0;
                                id = string.Empty;
                                nm = string.Empty;
                                mi = string.Empty;
                                ca = string.Empty;
                                try
                                {
                                    await selectVideo(); //por aca
                                    nm = string.Empty;
                                }
                                catch (Exception ex)
                                {
                                    //no hagas nada
                                }
                            }
                            else //cuando no se encuentre un video con la fecha actual
                            {
                                using (var lines = new StreamReader(ruta2))
                                {
                                    while ((line = lines.ReadLine()) != null) //se lee linea por linea del archivo parametros.txt y lo guarda en line
                                    {
                                        if (line[0] == '<' && line[1] == 'n' && line[2] == 'm' && line[3] == '>')
                                        {
                                            isSaving = true;
                                            nm = string.Empty;
                                            for (int i = 4; i < line.Length; i++)
                                            {
                                                if (isSaving == true)
                                                {
                                                    if (line[i] == '<')
                                                    {
                                                        isSaving = false;
                                                    }
                                                    else
                                                    {
                                                        nm += line[i];
                                                    }
                                                }
                                            }
                                        }
                                        else if (line[0] == '<' && line[1] == 'm' && line[2] == 'i' && line[3] == '>')
                                        {
                                            lista2.Add(nm);
                                            mi = string.Empty;
                                            nm = string.Empty;
                                        }
                                        else
                                        {
                                            mi = string.Empty;
                                            nm = string.Empty;
                                        }
                                    }
                                }
                                mi = string.Empty;
                                try
                                {
                                    await selectVideo();
                                    nm = string.Empty;
                                }
                                catch (Exception ex)
                                {
                                    //no hagas nada
                                }
                            }
                        }
                        /////////////////////////////////////////////////////////////////////
                    }
                }
                catch (Exception)
                {
                    //no hagas nada
                }

            }

            async Task stayalert()
            {
                try
                {
                    using (WebClient pet = new WebClient())
                    {
                        if (CrossConnectivity.Current.IsConnected)
                        {
                            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create("http://www.google.com");
                            myRequest.Timeout = 10000;
                            HttpWebResponse response = (HttpWebResponse)myRequest.GetResponse();
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                response.Close();
                            }
                            else
                            {
                                response.Close();
                            }

                            pm = (PowerManager)context.GetSystemService(PowerService);
                            //wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "stay awake gently");
                            //wakeLock.Acquire();
                            if (CrossConnectivity.Current.IsConnected)
                            {
                                inten = context.PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
                                context.StartActivity(inten);
                                //wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "stay awake gently");
                                //wakeLock.Acquire();
                            }
                            else
                            {
                                pm = (PowerManager)context.GetSystemService(Context.PowerService);
                                wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                                wake.Acquire();
                                wake.Release();
                            }
                        }
                        else
                        {
                            pm = (PowerManager)context.GetSystemService(Context.PowerService);
                            wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                            wake.Acquire();
                            wake.Release();
                        }
                    }
                }
                catch (Exception Ex)
                {
                    //wifiinfo = wifi.ConnectionInfo;
                    //level = WifiManager.CalculateSignalLevel(wifiinfo.Rssi, 11); //calcula la potencia de la señal wifi
                    if (CrossConnectivity.Current.IsConnected)
                    {
                        inten = context.PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
                        context.StartActivity(inten);
                    }
                    else
                    {
                        pm = (PowerManager)context.GetSystemService(Context.PowerService);
                        wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                        wake.Acquire();
                        wake.Release();
                    }
                }
            }

            async Task selectVideo()
            {
                while (recorre < lista2.Count)
                {
                    aux_nm = lista2[recorre];
                    string rut_video = Path.Combine(path_archivos, aux_nm); //toma ruta del video
                    if (File.Exists(rut_video))
                    {
                        rutVideodata = rut_video;
                        if (!localVideoView.IsPlaying)
                        {
                            playing = true;
                            hour_enabled = false;
                            recorre++;
                            VideoPlay(localVideoView, rut_video);
                            break;
                        }
                    }
                    else
                    {
                        recorre++;
                    }
                }
                if (lista2.Count == 1)
                {
                    recorre = 0;
                }
                else if (recorre == lista2.Count)
                {
                    recorre = 0;
                    lista2.Clear(); //reinicia la lista 
                    await ReadFile(); //comienza otra vez con la lectura del archivo
                }
            }

            async Task VideoPlay(VideoView video_main, string ruta)
            {
                try
                {
                    video_main.SetOnPreparedListener(new VideoLoop());
                    if (!video_main.IsPlaying)
                    {
                        try
                        {
                            video_main.SetVideoPath(ruta);
                            video_main.RequestFocus();
                            video_main.Start(); //problemas cuando se reproduce el video por segunda vez
                        }
                        catch (Exception ex)
                        {
                            Toast.MakeText(Application.Context, "no se puede reproduccir el video", ToastLength.Long).Show();
                        }
                    }
                }
                catch (Exception ex)
                {
                    //no hagas nada
                }
            }
        }

        public class VideoLoop : Java.Lang.Object, MediaPlayer.IOnPreparedListener
        {
            public void OnPrepared(MediaPlayer mp)
            {
                mp.Looping = true;
            }
        }

        //protected override async void OnDestroy()
        //{
        //    base.OnDestroy();
        //    var combina = Path.Combine(path_arch, "historial.txt");
        //    if (!File.Exists(Path.Combine(combina)))
        //    {
        //        using (var escribe = new StreamWriter(combina, true))
        //        {
        //            escribe.WriteLine("se destruye");
        //        }
        //    }
        //    else
        //    {
        //        using (var escribe = new StreamWriter(combina, true))
        //        {
        //            escribe.WriteLine("se destruye");
        //        }
        //    }
        //}

        //protected override async void OnStop()
        //{
        //    base.OnStop();
        //    try
        //    {
        //        var combina = Path.Combine(path_arch, "historial.txt");
        //        date = DateTime.Today;
        //        dia = date.Day.ToString();
        //        mes = date.Month.ToString();
        //        año = date.Year.ToString();
        //        fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual

        //        time = DateTime.Now.ToLocalTime();
        //        hora = time.Hour.ToString();
        //        min = time.Minute.ToString();
        //        string tiempo = hora + ":" + min;
        //        if (!File.Exists(Path.Combine(combina)))
        //        {
        //            using (var escribe = new StreamWriter(combina, true))
        //            {
        //                escribe.WriteLine("se detuvo");
        //                escribe.WriteLine(fecha_actual);
        //                escribe.WriteLine(tiempo);
        //            }
        //        }
        //        else
        //        {
        //            using (var escribe = new StreamWriter(combina, true))
        //            {
        //                escribe.WriteLine("se detuvo");
        //                escribe.WriteLine(fecha_actual);
        //                escribe.WriteLine(tiempo);
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        var combina = Path.Combine(path_arch, "historial.txt");
        //        if (!File.Exists(Path.Combine(combina)))
        //        {
        //            using (var escribe = new StreamWriter(combina, true))
        //            {
        //                escribe.WriteLine("excepcion OnStop");
        //            }
        //        }
        //        else
        //        {
        //            using (var escribe = new StreamWriter(combina, true))
        //            {
        //                escribe.WriteLine("excepcion OnStop");
        //            }
        //        }
        //    }
        //}

        protected override async void OnResume()
        {
            base.OnResume();
            try
            {
                //var combina = Path.Combine(path_arch, "historial.txt");
                //if (!Directory.Exists(path_arch))
                //{
                //    Directory.CreateDirectory(path_arch);
                //}
                //if (!File.Exists(combina))
                //{
                //    File.WriteAllText(combina, string.Empty);
                //}
                //date = DateTime.Today;
                //dia = date.Day.ToString();
                //mes = date.Month.ToString();
                //año = date.Year.ToString();
                //fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual

                //time = DateTime.Now.ToLocalTime();
                //hora = time.Hour.ToString();
                //min = time.Minute.ToString();
                //string tiempo = hora + ":" + min;
                //if (!File.Exists(Path.Combine(combina)))
                //{
                //    using (var escribe = new StreamWriter(combina, true))
                //    {
                //        escribe.WriteLine("se reanuda");
                //        escribe.WriteLine(fecha_actual);
                //        escribe.WriteLine(tiempo);
                //    }
                //}
                //else
                //{
                //    using (var escribe = new StreamWriter(combina, true))
                //    {
                //        escribe.WriteLine("se reanuda");
                //        escribe.WriteLine(fecha_actual);
                //        escribe.WriteLine(tiempo);
                //    }
                //}

                bool isInBackground;
                RunningAppProcessInfo myProcess = new RunningAppProcessInfo();
                GetMyMemoryState(myProcess);
                isInBackground = myProcess.Importance != Importance.Foreground;
                if (isInBackground)
                {
                    var intento = new Intent(Application.Context, typeof(MainActivity));
                    intento.AddCategory(Intent.CategoryLauncher);
                    intento.AddFlags(ActivityFlags.NewTask);
                    Application.Context.StartActivity(intento);
                }
            }
            catch(Exception)
            {
                //var combina = Path.Combine(path_arch, "historial.txt");
                //if (!File.Exists(Path.Combine(combina)))
                //{
                //    using (var escribe = new StreamWriter(combina, true))
                //    {
                //        escribe.WriteLine("excepcion OnResume");
                //    }
                //}
                //else
                //{
                //    using (var escribe = new StreamWriter(combina, true))
                //    {
                //        escribe.WriteLine("excepcion OnResume");
                //    }
                //}
            }
        }
    }
}