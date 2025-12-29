using Android.App;
using Android.Content;
using Android.Widget;
using AView = Android.Views.View;

namespace AlHatorah;

// Android-specific long-click listener for WebView. Shows a simple dialog with options when a link is long-pressed.
public class WebViewLongClickListener(Context context) : Java.Lang.Object, AView.IOnLongClickListener {
  public bool OnLongClick(AView v) {
    try {
      if (v is Android.Webkit.WebView webView) {
        Android.Webkit.WebView.HitTestResult hit = webView.GetHitTestResult();
        string extra = hit?.Extra;

        // If there's no extra (no link/image/etc), don't consume the event
        if (string.IsNullOrEmpty(extra)) {
          return false;
        }

        string[] items = ["Open link", "Copy link", "Share link"];

        // Prefer Activity context for dialogs
        AlertDialog.Builder builder = context is Activity act
          ? new AlertDialog.Builder(act)
          : new AlertDialog.Builder(context);

        // Create a selectable, truncated TextView to show the URL at the top
        TextView titleView = new(context) {
          Text = extra,
          TextSize = 14f,
        };
        titleView.SetPadding(24, 18, 24, 6);
        titleView.SetTextIsSelectable(true);
        titleView.SetMaxLines(3);
        titleView.Ellipsize = Android.Text.TextUtils.TruncateAt.End;

        builder.SetCustomTitle(titleView);

        builder.SetItems(items, (sender, args) => {
          int which = args.Which;
          switch (which) {
            case 0:
              // Open link in external browser
              Intent intent = new(Intent.ActionView, Android.Net.Uri.Parse(extra));
              intent.AddFlags(ActivityFlags.NewTask);
              context.StartActivity(intent);
              break;
            case 1:
              // Copy link to clipboard using MAUI Clipboard API
              _ = Clipboard.SetTextAsync(extra);
              Toast.MakeText(context, "Link copied", ToastLength.Short)?.Show();
              break;
            case 2:
              // Share link using Android share sheet
              Intent share = new(Intent.ActionSend);
              share.SetType("text/plain");
              share.PutExtra(Intent.ExtraText, extra);
              share.AddFlags(ActivityFlags.NewTask);
              context.StartActivity(Intent.CreateChooser(share, "Share link"));
              break;
            default:
              break;
          }
        });

        builder.SetCancelable(true);
        AlertDialog dialog = builder.Create();
        dialog.Show();

        return true;
      }
    } catch { }

    return false;
  }
}
