using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace monitor_inteligente
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    class BootComplete : BroadcastReceiver
    {
        #region implemented abstract members of BroadcastReceiver
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action.Equals(Intent.ActionBootCompleted))
            {
                var serviceIntent = new Intent(context, typeof(MainActivity));
                serviceIntent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(serviceIntent);
            }
        }
        #endregion
    }
}