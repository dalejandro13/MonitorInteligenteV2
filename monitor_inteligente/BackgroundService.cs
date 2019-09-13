using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.Connectivity;
using static Android.App.ActivityManager;
using static Android.OS.PowerManager;

namespace monitor_inteligente
{
    [Service]
    public class BackgroundService : Service
    {
        string path_archivos = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).ToString() + "/archivos";
        string combina = string.Empty;
        WifiManager wm;
        WifiManager.WifiLock wifiLock;
        public Intent intent;
        PowerManager pm;
        WakeLock wake;
        //Singleton sgl;
        System.Timers.Timer Tbusy;
        DateTime time, date;
        string dia, mes, año, fecha_actual, hora, min;
        bool enableMethod = true;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            //WifiManager wm = (WifiManager)GetSystemService(WifiService);
            //wifiLock = wm.CreateWifiLock(WifiMode.FullHighPerf, "keep wifi on");
            //wifiLock.Acquire();
            //Tbusy = new System.Timers.Timer();
            //pm = (PowerManager)GetSystemService(Context.PowerService);
            //wake = pm.NewWakeLock(WakeLockFlags.Partial, "stay awake gently");
            //wake.Acquire();
            enableMethod = true;
            combina = Path.Combine(path_archivos, "historial.txt"); //agregado
            //obj = new Singleton();  //se declara clase Singleton
            //Initializetimer();
            Task.Run(async () => await MethodRecursive());
            return StartCommandResult.Sticky;
        }

        async Task MethodRecursive()
        {
            try
            {
                var connectivityManager = (ConnectivityManager)Application.Context.GetSystemService(Context.ConnectivityService);
                NetworkInfo networkInfo = connectivityManager.ActiveNetworkInfo;
                if (networkInfo != null && networkInfo.IsConnected)
                {
                    date = DateTime.Today;
                    dia = date.Day.ToString();
                    mes = date.Month.ToString();
                    año = date.Year.ToString();
                    fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual
                    time = DateTime.Now.ToLocalTime();
                    hora = time.Hour.ToString();
                    min = time.Minute.ToString();
                    string tiempo = hora + ":" + min;
                    if (!File.Exists(combina))
                    {
                        using (StreamWriter file = new StreamWriter(combina, true))
                        {
                            file.WriteLine("se ejecuta en CheckForInternetConnection");
                            file.WriteLine("fecha: " + fecha_actual);
                            file.WriteLine("hora: " + tiempo);
                        }
                    }
                    else
                    {
                        using (StreamWriter file = new StreamWriter(combina, true))
                        {
                            file.WriteLine("se ejecuta en CheckForInternetConnection");
                            file.WriteLine("fecha: " + fecha_actual);
                            file.WriteLine("hora: " + tiempo);
                        }
                    }

                    pm = (PowerManager)GetSystemService(Context.PowerService);
                    wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease | WakeLockFlags.Partial, "wakeup device");
                    wake.Acquire();
                    wake.Release();
                    intent = PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
                    StartActivity(intent);
                }
                else
                {
                    enableMethod = false;
                }
            }
            catch (Exception ex)
            {

            }
            if (enableMethod == true)
            {
                await Task.Delay(1000 * 60 * 4);
                await MethodRecursive();
            }
        }

        //async Task Initializetimer()
        //{
            
        //    if (Tbusy.Enabled == false)
        //    {
        //        Tbusy.Interval = 1000 * 60 * 4;
        //        Tbusy.Elapsed += new System.Timers.ElapsedEventHandler(CheckForInternetConnection);
        //        Tbusy.Start();
        //    }
        //}

        //private void CheckForInternetConnection(object sender, ElapsedEventArgs e)
        //{
        //    try
        //    {
        //        var connectivityManager = (ConnectivityManager)(Application.Context.GetSystemService(Context.ConnectivityService));
        //        NetworkInfo networkInfo = connectivityManager.ActiveNetworkInfo;
        //        if (networkInfo != null && networkInfo.IsConnected)
        //        {
        //            date = DateTime.Today;
        //            dia = date.Day.ToString();
        //            mes = date.Month.ToString();
        //            año = date.Year.ToString();
        //            fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual
        //            time = DateTime.Now.ToLocalTime();
        //            hora = time.Hour.ToString();
        //            min = time.Minute.ToString();
        //            string tiempo = hora + ":" + min;
        //            if (!File.Exists(combina))
        //            {
        //                using (StreamWriter file = new StreamWriter(combina))
        //                {
        //                    file.WriteLine("se ejecuta en CheckForInternetConnection");
        //                    file.WriteLine("fecha: " + fecha_actual);
        //                    file.WriteLine("hora: " + tiempo);
        //                }
        //            }
        //            else
        //            {                    
        //                using (StreamWriter file = new StreamWriter(combina))
        //                {
        //                    file.WriteLine("se ejecuta en CheckForInternetConnection");
        //                    file.WriteLine("fecha: " + fecha_actual);
        //                    file.WriteLine("hora: " + tiempo);
        //                }
        //            }
        //            //var sgl = Singleton.Instance;
        //            //sgl.OnOff = true;
        //            //pm = (PowerManager)GetSystemService(Context.PowerService);
        //            //wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
        //            //wake.Acquire();
        //            //wake.Release();
        //            //intent = PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
        //            //StartActivity(intent);
        //        }
        //        else
        //        {
        //            Tbusy.Dispose();
        //            Tbusy.Stop();
        //        }
        //    }
        //    catch(Exception ex)
        //    {

        //    }
        //}

        //private void CheckForInternetConnection()
        //{
        //    try
        //    {
        //        if (enableInternet == true)
        //        {
        //            pm = (PowerManager)GetSystemService(Context.PowerService);
        //            wake = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
        //            wake.Acquire();
        //            wake.Release();
        //            intent = PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
        //            StartActivity(intent);
        //        }
        //    }
        //    catch
        //    {
        //        enableInternet = false;
        //    }

        //    if (enableInternet == true)
        //    {
        //        for (int i = 0; i <= 1560; i++)
        //        {
        //            //await Task.Delay(30);
        //            Thread.Sleep(30);
        //        }
        //        CheckForInternetConnection();
        //    }
        //}

        public override bool StopService(Intent name)
        {
            date = DateTime.Today;
            dia = date.Day.ToString();
            mes = date.Month.ToString();
            año = date.Year.ToString();
            fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual
            time = DateTime.Now.ToLocalTime();
            hora = time.Hour.ToString();
            min = time.Minute.ToString();
            string tiempo = hora + ":" + min;
            if (!File.Exists(combina))
            {
                using (StreamWriter file = new StreamWriter(combina, true))
                {
                    file.WriteLine("se ejecuta en Stopservice");
                    file.WriteLine("fecha: " + fecha_actual);
                    file.WriteLine("hora: " + tiempo);
                }
            }
            else
            {
                using (StreamWriter file = new StreamWriter(combina, true))
                {
                    file.WriteLine("se ejecuta en Stopservice");
                    file.WriteLine("fecha: " + fecha_actual);
                    file.WriteLine("hora: " + tiempo);
                }
            }

            //Tbusy.Dispose();
            //Tbusy.Stop();
            enableMethod = false;
            return base.StopService(name);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            date = DateTime.Today;
            dia = date.Day.ToString();
            mes = date.Month.ToString();
            año = date.Year.ToString();
            fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual
            time = DateTime.Now.ToLocalTime();
            hora = time.Hour.ToString();
            min = time.Minute.ToString();
            string tiempo = hora + ":" + min;
            if (!File.Exists(combina))
            {
                using (StreamWriter file = new StreamWriter(combina, true))
                {
                    file.WriteLine("se ejecuta en OnDestroy");
                    file.WriteLine("fecha: " + fecha_actual);
                    file.WriteLine("hora: " + tiempo);
                }
            }
            else
            {
                using (StreamWriter file = new StreamWriter(combina, true))
                {
                    file.WriteLine("se ejecuta en OnDestroy");
                    file.WriteLine("fecha: " + fecha_actual);
                    file.WriteLine("hora: " + tiempo);
                }
            }
            enableMethod = false;
            //Tbusy.Dispose();
            //    bool isInBackground;
            //    RunningAppProcessInfo myProcess = new RunningAppProcessInfo();
            //    GetMyMemoryState(myProcess);
            //    isInBackground = myProcess.Importance != Importance.Foreground;
            //    if (isInBackground)
            //    {
            //        var intento = new Intent(Application.Context, typeof(MainActivity));
            //        intento.AddCategory(Intent.CategoryLauncher);
            //        intento.AddFlags(ActivityFlags.NewTask);
            //        Application.Context.StartActivity(intento);
            //    }

            //    //wifiLock.Release();
            //    //wake.Release();
        }
    }
}