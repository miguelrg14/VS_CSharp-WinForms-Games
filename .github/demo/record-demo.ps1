# Plays the games like a user (mouse clicks found through UI Automation) and saves a screenshot after
# every step. The screenshots are turned into the README's animated demos.
#
# Usage: record-demo.ps1 -Exe path\to\OopGames.Desktop.exe -Out frames
param(
    [Parameter(Mandatory)] [string] $Exe,
    [Parameter(Mandatory)] [string] $Out
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Native {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    // The visible frame of a window, without the invisible resize border that GetWindowRect includes.
    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out RECT rect, int size);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public int dmFields, dmPositionX, dmPositionY, dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }

    [DllImport("user32.dll")] public static extern bool EnumDisplaySettings(string device, int mode, ref DEVMODE devMode);
    [DllImport("user32.dll")] public static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

    public static int SetResolution(int width, int height) {
        var mode = new DEVMODE();
        mode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        EnumDisplaySettings(null, -1, ref mode);
        mode.dmPelsWidth = width;
        mode.dmPelsHeight = height;
        mode.dmFields = 0x80000 | 0x100000; // DM_PELSWIDTH | DM_PELSHEIGHT
        return ChangeDisplaySettings(ref mode, 0);
    }
}
'@
[Native]::SetProcessDPIAware() | Out-Null
# The runner's screen is 1024x768 by default, too small for the bigger windows.
Write-Host "Set resolution: $([Native]::SetResolution(1920, 1080))"
Start-Sleep -Seconds 2

$UIA = [System.Windows.Automation.AutomationElement]
$Tree = [System.Windows.Automation.TreeScope]
$Prop = [System.Windows.Automation.AutomationElement]
$Type = [System.Windows.Automation.ControlType]

New-Item -ItemType Directory -Force -Path $Out | Out-Null
$script:frame = 0
$script:manifest = @()

# Top-level windows of the app. A window opened with an owner (a game opened from the launcher, a
# message box opened from a game) is listed by UI Automation under its owner, so look there as well.
function Find-AppWindow([scriptblock] $test) {
    $mine = New-Object System.Windows.Automation.PropertyCondition($Prop::ProcessIdProperty, $script:app.Id)
    $queue = [System.Collections.Generic.Queue[object]]::new()
    foreach ($w in $UIA::RootElement.FindAll($Tree::Children, $mine)) { $queue.Enqueue($w) }
    $windowType = New-Object System.Windows.Automation.PropertyCondition($Prop::ControlTypeProperty, $Type::Window)
    while ($queue.Count -gt 0) {
        $w = $queue.Dequeue()
        if (& $test $w) { return $w }
        foreach ($child in $w.FindAll($Tree::Children, $windowType)) { $queue.Enqueue($child) }
    }
    return $null
}

function Wait-Window([string] $name, [int] $seconds = 30) {
    for ($i = 0; $i -lt $seconds * 4; $i++) {
        $window = Find-AppWindow { param($w) $w.Current.Name -eq $name }
        if ($window) { return $window }
        Start-Sleep -Milliseconds 250
    }
    throw "Window '$name' did not appear."
}

function Find-Button($window, [string] $name, [int] $index = 0) {
    $condition = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition($Prop::ControlTypeProperty, $Type::Button)),
        (New-Object System.Windows.Automation.PropertyCondition($Prop::NameProperty, $name)))
    $buttons = @($window.FindAll($Tree::Descendants, $condition))
    $sorted = @($buttons | Sort-Object { $_.Current.BoundingRectangle.Y }, { $_.Current.BoundingRectangle.X })
    if ($sorted.Count -le $index) { throw "Button '$name' #$index not found." }
    return $sorted[$index]
}

function Click($element) {
    $r = $element.Current.BoundingRectangle
    [Native]::SetCursorPos([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2)) | Out-Null
    Start-Sleep -Milliseconds 120
    [Native]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)   # left down
    [Native]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)   # left up
    Start-Sleep -Milliseconds 450
}

# Saves the area of $window and how long the frame should stay on screen.
function Snap($window, [string] $clip, [double] $seconds = 1.2) {
    Start-Sleep -Milliseconds 250
    $rect = New-Object Native+RECT
    [Native]::DwmGetWindowAttribute([IntPtr]$window.Current.NativeWindowHandle, 9, [ref]$rect, 16) | Out-Null
    $x = $rect.Left; $y = $rect.Top; $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    $bitmap = New-Object System.Drawing.Bitmap $w, $h
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($x, $y, 0, 0, $bitmap.Size)
    $file = "{0}-{1:D3}.png" -f $clip, $script:frame
    $bitmap.Save((Join-Path $Out $file), [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose()
    $script:manifest += "$file,$seconds"
    $script:frame++
}

# If the app opened a message box, show it in the demo and close it with Enter.
function Handle-Dialogs($window, [string] $clip, [double] $seconds = 1.6) {
    for ($n = 0; $n -lt 3; $n++) {
        $dialog = Find-AppWindow { param($w) $w.Current.ClassName -eq '#32770' }
        if (-not $dialog) { return }
        Snap $window $clip $seconds
        [Native]::SetForegroundWindow([IntPtr]$dialog.Current.NativeWindowHandle) | Out-Null
        [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
        Start-Sleep -Milliseconds 500
    }
}

function Close-Window($window) {
    $window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
    Start-Sleep -Milliseconds 800
}

function Open-Project($launcher, [int] $index, [string] $title) {
    Click (Find-Button $launcher 'Open' $index)
    $window = Wait-Window $title
    [Native]::SetForegroundWindow([IntPtr]$window.Current.NativeWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 600
    return $window
}

$script:app = Start-Process -FilePath $Exe -PassThru
$launcher = Wait-Window 'OOP Games' 60
Start-Sleep -Seconds 2
Snap $launcher 'launcher' 3

# Blackjack: each action passes the turn to the other player, and two passes in a row end the round.
$game = Open-Project $launcher 4 'Blackjack'
Snap $game 'blackjack' 2
$round = @('Draw card', 'Draw card', 'Draw card', 'Draw card', 'Pass', 'Pass')
$plan = @('Bet 1 coin') + $round + @('Draw card', 'Bet 1 coin') + $round + $round
foreach ($action in $plan) {
    $button = Find-Button $game $action
    if (-not $button.Current.IsEnabled) { break }
    Click $button
    Handle-Dialogs $game 'blackjack'
    Snap $game 'blackjack'
}
Close-Window $game

# Elemental tower: always use the element that beats the current enemy.
$game = Open-Project $launcher 2 'Elemental tower'
Snap $game 'tower' 2
$counter = @{ Fire = 'Water'; Water = 'Grass'; Grass = 'Fire' }
for ($turn = 0; $turn -lt 24; $turn++) {
    $texts = @($game.FindAll($Tree::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition($Prop::ControlTypeProperty, $Type::Text))) |
        ForEach-Object { $_.Current.Name })
    $enemy = $texts | Where-Object { $_ -match '\((Fire|Water|Grass)\)' } | Select-Object -First 1
    $element = if ($enemy -match '\((Fire|Water|Grass)\)') { $counter[$Matches[1]] } else { 'Fire' }
    $button = Find-Button $game "$element attack"
    if (-not $button.Current.IsEnabled) { break }
    Click $button
    Snap $game 'tower' 0.9
    Handle-Dialogs $game 'tower' 2.5
}
Close-Window $game

# Weapon arena: the first living unit on each side is selected automatically; attack until the battle ends.
$game = Open-Project $launcher 3 'Weapon arena'
Snap $game 'arena' 2
for ($turn = 0; $turn -lt 14; $turn++) {
    $button = Find-Button $game 'Attack!'
    if (-not $button.Current.IsEnabled) { break }
    Click $button
    Snap $game 'arena' 0.9
    Handle-Dialogs $game 'arena' 2.5
}
Close-Window $game

# Duel: take turns attacking.
$game = Open-Project $launcher 1 'Duel'
Snap $game 'duel' 2
for ($turn = 0; $turn -lt 8; $turn++) {
    Click (Find-Button $game 'Striker attacks')
    Snap $game 'duel' 0.9
    Click (Find-Button $game 'Guardian attacks')
    Snap $game 'duel' 0.9
}
Close-Window $game

$script:manifest | Set-Content (Join-Path $Out 'frames.csv')
Stop-Process -Id $script:app.Id -ErrorAction SilentlyContinue
Write-Host "Saved $($script:frame) frames to $Out"
