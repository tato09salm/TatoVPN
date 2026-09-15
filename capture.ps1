Add-Type -AssemblyName System.Windows.Forms, System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

# Load TatoVPN dll
$dllPath = "C:\Users\jeanm\Downloads\VPN\TatoVPN\TatoVPN_C#\bin\Debug\net8.0-windows\TatoVPN.dll"
$bytes = [System.IO.File]::ReadAllBytes($dllPath)
$asm = [System.Reflection.Assembly]::Load($bytes)
$formType = $asm.GetType("miVPN.Form1")
$form = [Activator]::CreateInstance($formType)

$form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
$form.Location = New-Object System.Drawing.Point(50, 50)
$form.Size = New-Object System.Drawing.Size(1090, 700) # Tamaño mínimo permitido

# Click btnNavDashboard
$navDashField = $formType.GetField("btnNavDashboard", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
$btnDash = $navDashField.GetValue($form)
$btnDash.PerformClick()

$form.Show()

# Process events for 2 seconds to let LiveCharts render
$sw = [System.Diagnostics.Stopwatch]::StartNew()
while ($sw.ElapsedMilliseconds -lt 2500) {
    [System.Windows.Forms.Application]::DoEvents()
    [System.Threading.Thread]::Sleep(50)
}

# Capture form to Bitmap
$bmp = New-Object System.Drawing.Bitmap($form.Width, $form.Height)
$form.DrawToBitmap($bmp, (New-Object System.Drawing.Rectangle(0, 0, $form.Width, $form.Height)))
$outPath = "C:\Users\jeanm\Downloads\VPN\TatoVPN\dashboard_ventana_chica.png"
$bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
$form.Close()
$form.Dispose()
Write-Output "Screenshot saved to $outPath"
