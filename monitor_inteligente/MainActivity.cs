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
//using static monitor_inteligente.MainActivity;

namespace monitor_inteligente
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        string path_archivos = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString() + "/archivos";
        //declaracion de las variables y los widgets
        int ctrl = 0, hora_actual = 0, hi_aux = 0, hf_aux = 0, ctrl_lectura = 0, ctrl_lea = 0, conteo_lineas = 0, min_aux = 0, desconect = 0;
        bool isSaving = false, ctrlWifi = true, hour_enabled = true, playing = false;
        public double bytesIn = 0, percentage = 0;
        string id, nm, hi, hf, ca, mi, nom1, dia, mes, año, fecha_actual, hora, min, aux_nm;
        public string linea1;
        long free_size = 0, totalbytesfiles = 0, bytesfile = 0, bytesfileserver = 0, tam_parametros = 0;
        public List<string> lista = new List<string>();
        public int recorre = 0;
        ProgressDialog progreso;
        private VideoView video_main;
        DateTime time, date;
        public VideoView Video_main { get => video_main; set => video_main = value; }
        public WebClient cliente = new WebClient();
        public WifiManager wifi;

        ///////////////////////////////////////////////////////////7
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            SupportActionBar.SetDisplayShowTitleEnabled(false); //quita el titulo de action bar
            SupportActionBar.Hide(); //quita el action bar

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
                    await Download();
                    PowerManager pm = (PowerManager)GetSystemService(Context.PowerService); //bloquear
                    pm.GoToSleep(30000);
                }
            }

            CrossConnectivity.Current.ConnectivityChanged += async delegate //detecta eventos de conexion wifi
            {
                if (CrossConnectivity.Current.IsConnected.ToString().Equals("false"))
                {
                    if (CrossConnectivity.Current.ConnectionTypes.Contains(ConnectionType.WiFi))
                    {
                        //si se pierde la conexion pero el wifi todavia funciona
                        Toast.MakeText(this, "conexion perdida con wifi", ToastLength.Long).Show();
                        ctrlWifi = false;
                    }
                }
                else
                {
                    if (CrossConnectivity.Current.ConnectionTypes.Contains(ConnectionType.WiFi))
                    {
                        // si wifi esta encendido y esta conectado
                        ctrlWifi = true;
                        Toast.MakeText(this, "conexion a internet establecida", ToastLength.Long).Show();
                        while (ctrlWifi)
                        {
                            await Download(); //cuando hay conexion a internet por wifi, inicia proceso para la descarga de archivos
                            Toast.MakeText(this, "LISTO2", ToastLength.Short).Show();
                            PowerManager pm = (PowerManager)GetSystemService(Context.PowerService); //bloquear
                            pm.GoToSleep(30000);
                            //KeyguardManager //desbloquear
                        }
                        await ReadFile();
                    }
                    else
                    {
                        //si hay conexion, pero no wifi
                        Toast.MakeText(this, "sin conexion por wifi", ToastLength.Long).Show();
                    }
                }
            };
            await ReadFile();
        }

        async Task Download()
        {
            //primero verificar espacio disponible en memoria para poder iniciar la descarga EN PROGRESO
            free_size = 0;
            totalbytesfiles = 0;
            long blocksize = 0;
            StatFs stat = new StatFs(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString() + "/archivos");
            blocksize = stat.BlockSize;
            long blockavailables = stat.AvailableBlocks;
            free_size = blocksize * blockavailables; //tamaño libre de memoria en tv Box

            if (wifi.IsWifiEnabled)
            {
                try
                {
                    //verifica que si la memoria del tvBox esta llena
                    using (cliente.OpenRead(new System.Uri("https://s3-sa-east-1.amazonaws.com/flexolumens/Test/parametros.txt")))
                    {
                        tam_parametros = Convert.ToInt64(cliente.ResponseHeaders["Content-Length"]); //mido el tamaño del archivo que esta en el servido                        
                    }
                    if (free_size <= tam_parametros)
                    {
                        Toast.MakeText(this, "no hay espacio suficiente en la memoria", ToastLength.Long).Show();
                        //rutina para borrar todo el contenido en el interior de la carpeta, mas no borrar la propia carpeta
                        var list = Directory.GetFiles(path_archivos, "*.mp4");
                        if (list.Length > 0)
                        {
                            for (int i = 0; i < list.Length; i++)
                            {
                                File.Delete(list[i]);
                            }
                        }
                    }
                    //descarga el archivo que contiene los parametros
                    cliente.DownloadFileCompleted += new AsyncCompletedEventHandler(completado);
                    cliente.DownloadProgressChanged += new DownloadProgressChangedEventHandler(cargando);
                    await cliente.DownloadFileTaskAsync(new System.Uri("https://s3-sa-east-1.amazonaws.com/flexolumens/Test/parametros.txt"), path_archivos + "/parametros.txt");
                    await Task.Delay(3000);
                    progreso.Dismiss();
                    //Leer archivo en tvBox para identificar el Bus
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                }
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
                                            var pathserver = "https://s3-sa-east-1.amazonaws.com/flexolumens/Test/" + nm;
                                            using (cliente.OpenRead(new System.Uri(pathserver))) //comparar tamaño del archivo del servidor con el que tiene internamente en la memoria
                                            {
                                                bytesfile = Convert.ToInt64(cliente.ResponseHeaders["Content-Length"]); //mido el tamaño del archivo que esta en el servidor
                                                totalbytesfiles = totalbytesfiles + bytesfile;
                                                nm = string.Empty;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
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
                                                isSaving = false;
                                                var pathserver = "https://s3-sa-east-1.amazonaws.com/flexolumens/Test/" + nm;
                                                var pathvideo = path_archivos + "/" + nm;

                                                if (File.Exists(pathvideo))
                                                {
                                                    try
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
                                                            //descarga el video del servidor 
                                                            cliente.DownloadFileCompleted += new AsyncCompletedEventHandler(completado);
                                                            cliente.DownloadProgressChanged += new DownloadProgressChangedEventHandler(cargando);
                                                            progreso.SetMessage("Descargando " + nm);
                                                            progreso.Show();
                                                            await cliente.DownloadFileTaskAsync(new System.Uri(pathserver), pathvideo);
                                                            await Task.Delay(3000);
                                                            progreso.Dismiss();
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                                                    }
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        using (cliente.OpenRead(new System.Uri(pathserver)))
                                                        {
                                                            bytesfileserver = Convert.ToInt64(cliente.ResponseHeaders["Content-Length"]); //mido el tamaño del archivo que esta en el servidor
                                                        }
                                                        //inicia descarga del video en el servidor 
                                                        cliente.DownloadProgressChanged += new DownloadProgressChangedEventHandler(cargando);
                                                        cliente.DownloadFileCompleted += new AsyncCompletedEventHandler(completado);
                                                        progreso.SetMessage("Descargando " + nm);
                                                        progreso.Show();
                                                        await cliente.DownloadFileTaskAsync(new System.Uri(pathserver), pathvideo);
                                                        await Task.Delay(3000);
                                                        progreso.Dismiss();
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Toast.MakeText(this, e.Message, ToastLength.Long).Show();
                                                    }
                                                }
                                                ctrlWifi = false; //para salir del ciclo while
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
            else
            {
                blocksize = 0;
                blockavailables = 0;
                free_size = 0;
                await Task.Delay(3000);
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

        private async void completado(object sender, AsyncCompletedEventArgs e)
        {
            bytesIn = 0;
            percentage = 0;
            bytesfileserver = 0;
            nm = string.Empty;
        }

        private async void cargando(object sender, DownloadProgressChangedEventArgs e)
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
            try
            {
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
                                //time = new DateTime(dt1.Hour, dt1.Minute, 0);
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
                                    //nom = v[M].Name; //nombre del video
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
                                            //Toast.MakeText(this, "reproduciendo otro video", ToastLength.Long).Show();
                                            rut_video = Path.Combine(path_archivos, aux_nm); //toma ruta del video
                                        }
                                        nm = string.Empty;
                                    }
                                    catch (Exception ex)
                                    {
                                        Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
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
                        //lista.Clear(); //limpia la lista para llenarse nuevamente
                        //lista_min.Clear();
                        ///obtiene los nombres de los videos para almacenarlos en una lista
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
                                        if (ca == fecha_actual && mi != string.Empty)
                                        {
                                            lista.Add(nm);
                                            //lista_min.Add(mi);
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
                            Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                        }
                    }
                }
            }
            catch (Exception)
            {
                Toast.MakeText(this, "error de reproduccion", ToastLength.Long).Show();
            }
        }

        async Task selectVideo()
        {
            while (recorre < lista.Count)
            {
                aux_nm = lista[recorre];
                //mi = lista_min[recorre];
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
            }
            if (recorre == lista.Count)
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
                video_main.SetOnCompletionListener(new VideoLoop1(video_main, lista, recorre));
                if (!video_main.IsPlaying)
                {
                    video_main.SetVideoPath(ruta);
                    video_main.RequestFocus();
                    video_main.Start();
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
            }

            //if (hour_enabled == false)
            //{
            //var t = Task.Run(async() => await setTime());
            //t.Wait();
            //}
        }

        //private async Task setTime()
        //{
        //    retriever = new MediaMetadataRetriever();
        //    retriever.SetDataSource(rutVideodata);
        //    string tiempo = retriever.ExtractMetadata(MediaMetadataRetriever.MetadataKeyDuration);
        //    double timemillis = double.Parse(tiempo);
        //    double Convertminutes = timemillis / 60000; //conversion a minutos

        //if (mi != "0" && Convertminutes >= double.Parse(mi))
        //{
        //    timer = new Timer(detener, 1, TimeSpan.FromMinutes(double.Parse(mi)), TimeSpan.FromMinutes(Convert.ToDouble(mi)));
        //    mi = string.Empty;
        //}
        //else //reproduccion de videos por completo
        //{
        //    timer.Dispose();
        //}
        //}

        //public async void detener(object state)
        //{
        //    if (video_main.IsPlaying)
        //    {
        //        //video_main.StopPlayback();
        //        video_main.Pause();
        //        video_main.SeekTo(0);
        //        playing = false;
        //        mi = string.Empty;
        //        await selectVideo();
        //    }
        //}

        public class VideoLoop1 : Java.Lang.Object, MediaPlayer.IOnCompletionListener
        {
            string path_archivos = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString() + "/archivos";
            DateTime time, date;
            int M = 0, S = 0, ctrl = 0, hora_actual = 0, hi_aux = 0, hf_aux = 0, ctrl_lectura = 0, ctrl_lea = 0, conteo_lineas = 0, min_aux = 0, recorre = 1;
            bool isSaving = false, ciclo = true, hour_enabled = true, playing = false;
            public string id, nm, hi, hf, ca, mi, nom, nom1, dia, mes, año, fecha_actual, hora, min, aux_nm, rutVideodata;
            VideoView localVideoView;
            List<string> lista2;

            //List<string> lista_min = new List<string>();

            //Timer timer;
            //MediaMetadataRetriever retriever;

            public VideoLoop1(VideoView videoView, List<string> videos, int counter)
            {
                localVideoView = videoView; //puente entre videoview de la clase mainactivity y videoloop1
                lista2 = videos;
                recorre = counter;
            }

            public void OnCompletion(MediaPlayer mp)
            {
                try
                {
                    mp.Stop();
                    if (recorre == lista2.Count && hour_enabled == false)
                    {
                        recorre = 0;
                        lista2.Clear(); //reinicia nuevamente a lista2
                        ReadFile(); //comienza otra vez con la lectura del archivo
                    }
                    else
                    {
                        ReadFile();
                    }
                }
                catch (Exception ex)
                {

                }
            }

            async Task ReadFile()
            {
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
                                    //time = new DateTime(dt1.Hour, dt1.Minute, 0);
                                    hora = time.Hour.ToString();
                                    min = time.Minute.ToString();
                                    hora_actual = Convert.ToInt32(hora);
                                    string hora_actual1 = hora + ":" + min;
                                    hi_aux = Convert.ToInt32(hi);
                                    hf_aux = Convert.ToInt32(hf);
                                    min_aux = Convert.ToInt32(min);

                                    if (ca == fecha_actual && (hora_actual >= hi_aux && hora_actual < hf_aux)) // si el archivo tiene la fecha actual y la hora actual esta dentro del rango que muestra ese archivo, entonces reproduce el video
                                    {
                                        lista2.Clear();
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
                                                    //DeleteFile(path_archivos, "check.flexo");
                                                }
                                            }
                                            else
                                            {
                                                Toast.MakeText(Application.Context, "no hay videos para rerpoducir", ToastLength.Long).Show();
                                                //Toast.MakeText(Application.Context, "reproduciendo otro video", ToastLength.Long).Show();
                                                rut_video = Path.Combine(path_archivos, aux_nm); //toma ruta del video
                                            }
                                            nm = string.Empty;
                                        }
                                        catch (Exception ex)
                                        {
                                            Toast.MakeText(Application.Context, ex.Message, ToastLength.Long).Show();
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
                            hour_enabled = false; //si no encuentra reproduccion por horas
                            //poner alguna rutina que reproduzca algun video cuando en el archivo no se encuentra alguno con la fecha de hoy
                        }

                        if (hour_enabled == false)
                        {
                            ctrl = 0;
                            id = string.Empty;
                            nm = string.Empty;
                            mi = string.Empty;
                            ca = string.Empty;
                            //lista2.Clear(); //limpia la lista para llenarse nuevamente
                            //lista_min.Clear();
                            /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                            ///obtiene los nombres de los videos para almacenarlos en una lista
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
                                            if (ca == fecha_actual && mi != string.Empty)
                                            {
                                                lista2.Add(nm);
                                                //lista_min.Add(mi);
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
                                Toast.MakeText(Application.Context, ex.Message, ToastLength.Long).Show();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Toast.MakeText(Application.Context, "error de reproduccion", ToastLength.Long).Show();
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
                }
            }

            async Task VideoPlay(VideoView video_main, string ruta)
            {
                try
                {
                    video_main.SetOnPreparedListener(new VideoLoop());
                    if (!video_main.IsPlaying)
                    {
                        video_main.SetVideoPath(ruta);
                        video_main.Start(); //problemas cuando se reproduce el video por segunda vez
                    }
                }
                catch (Exception ex)
                {
                    Toast.MakeText(Application.Context, ex.Message, ToastLength.Long).Show();
                }
            }
        }

        public class VideoLoop : Java.Lang.Object, MediaPlayer.IOnPreparedListener
        {
            public void OnPrepared(MediaPlayer mp)
            {
                mp.Looping = false;
            }
        }

        protected override async void OnPause()
        {
            Toast.MakeText(this, "LISTO1", ToastLength.Short).Show();
        }
    }
}