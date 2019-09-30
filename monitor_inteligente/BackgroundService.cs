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
        int CountErase;
        WifiManager wm;
        WifiManager.WifiLock wifiLock;
        public Intent intent;
        PowerManager pm;
        WakeLock wake;
        DateTime time, date;
        string dia, mes, año, fecha_actual, hora, min, tiempo;
        bool enableMethod = true;
        Context context;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            enableMethod = true;
            CountErase = 0;
            combina = Path.Combine(path_archivos, "historial.txt"); //agregado
            pm = (PowerManager)GetSystemService(Context.PowerService);
            wake = pm.NewWakeLock(WakeLockFlags.Partial, "stay awake gently");
            Task.Run(async () => await MakeFolderAndFile());
            Task.Run(async () => await ActivateLock());
            Task.Run(async () => await RecursiveMethod());
            return StartCommandResult.Sticky;
        }

        async Task MakeFolderAndFile()
        {
            if (!Directory.Exists(path_archivos))
            {
                Directory.CreateDirectory(path_archivos);
            }
            if (!File.Exists(combina))
            {
                File.Create(combina);
            }
        }

        async Task ActivateLock()
        {
            wake.Acquire();
            intent = PackageManager.GetLaunchIntentForPackage("com.ssaurel.lockdevice");
            StartActivity(intent);
            //await Task.Delay(3000);
        }

        async Task RecursiveMethod()
        {
            try
            {
                var connectivityManager = (ConnectivityManager)Application.Context.GetSystemService(Context.ConnectivityService);
                NetworkInfo networkInfo = connectivityManager.ActiveNetworkInfo;
                var connect = await CheckForInternetConnection();

                ////date = DateTime.Now;
                ////fecha_actual = date.ToString("dd/MM/yyyy"); //obtengo la fecha actual
                ////tiempo = date.ToString("HH:mm tt");//hora + ":" + min;
                ////CountErase++;
                ////if (CountErase >= 60)
                ////{
                ////    if (File.Exists(combina))
                ////    {
                ////        File.WriteAllText(combina, string.Empty);
                ////        CountErase = 0;
                ////    }
                ////}

                ////if (File.Exists(combina))
                ////{
                ////    using (StreamWriter file = new StreamWriter(combina))
                ////    {
                ////        file.WriteLine("se ejecuta en CheckForInternetConnection");
                ////        file.WriteLine("fecha: " + fecha_actual);
                ////        file.WriteLine("hora: " + tiempo);
                ////    }
                ////}
                    
                //intent = PackageManager.GetLaunchIntentForPackage("com.flexolumens.serial"); //va al programa para ejecutar serial
                //StartActivity(intent);
                //StopSelf();

                await Task.Delay(7000);
                if (networkInfo != null && networkInfo.IsConnected && connect == true)
                {
                    await RecursiveMethod();
                }
                else
                {
                    wake.Release();
                    StopSelf();

                    //PowerManager pwm = (PowerManager)GetSystemService(Context.PowerService);
                    //WakeLock wkl = pwm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "wakeup device");
                    //wkl.Acquire();
                    //wkl.Release();

                    //intent = PackageManager.GetLaunchIntentForPackage("com.flexolumens.MonitorInteligente");
                    intent = new Intent(Application.Context, typeof(MainActivity));
                    intent.SetFlags(intent.Flags | ActivityFlags.NoHistory | ActivityFlags.NewTask);
                    intent.SetAction(Intent.ActionMain);
                    intent.AddCategory(Intent.CategoryLauncher);
                    Application.Context.StartActivity(intent);
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            date = DateTime.Now;
            fecha_actual = date.ToString("dd/MM/yyyy"); //obtengo la fecha actual
            tiempo = date.ToString("HH:mm tt");//hora + ":" + min;
            CountErase++;
            if (CountErase >= 60)
            {
                if (File.Exists(combina))
                {
                    File.WriteAllText(combina, string.Empty);
                    CountErase = 0;
                }
            }

            if (File.Exists(combina))
            {
                using (StreamWriter file = new StreamWriter(combina))
                {
                    file.WriteLine("se ejecuta en OnDestroy");
                    file.WriteLine("fecha: " + fecha_actual);
                    file.WriteLine("hora: " + tiempo);
                }
            }

            enableMethod = false;
            StopSelf();
        }

        public async Task<bool> CheckForInternetConnection()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://clients3.google.com/generate_204");
                request.Timeout = 2000;
                request.Method = "GET";
                var resp = request.GetResponse();
                if (resp != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}