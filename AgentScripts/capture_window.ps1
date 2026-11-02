param([string]$Title, [string]$Out)
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Text; using System.Runtime.InteropServices;
public class Cap {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc f, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public struct RECT { public int L, T, R, B; }
}
"@

$unityPid = (Get-Process Unity | Where-Object { $_.MainWindowTitle } | Select-Object -First 1).Id
$script:found = [IntPtr]::Zero
$cb = [Cap+EnumProc]{ param($h, $l)
  $sb = New-Object System.Text.StringBuilder 256
  [Cap]::GetWindowText($h, $sb, 256) | Out-Null
  $p = 0; [Cap]::GetWindowThreadProcessId($h, [ref]$p) | Out-Null
  if ($p -eq $unityPid -and [Cap]::IsWindowVisible($h) -and $sb.ToString() -like "*$Title*") { $script:found = $h; return $false }
  return $true }
[Cap]::EnumWindows($cb, [IntPtr]::Zero) | Out-Null
if ($script:found -eq [IntPtr]::Zero) { "not found"; exit 1 }
$r = New-Object Cap+RECT; [Cap]::GetWindowRect($script:found, [ref]$r) | Out-Null
$w = $r.R - $r.L; $hgt = $r.B - $r.T
$bmp = New-Object System.Drawing.Bitmap $w, $hgt
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc(); [Cap]::PrintWindow($script:found, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc); $g.Dispose()
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
"saved $w x $hgt"
