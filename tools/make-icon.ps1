# Generates the Blazma app icon as a multi-size .ico: the Blazma family hexagon
# (same as Blazma Boost / Blazma Get / Blazma Cyber) with a white crosshair.
# The source of truth is branding/logo.svg; this draws the same shapes in its 100x100 grid.
param([string]$OutDir)

Add-Type -AssemblyName System.Drawing

$top    = [System.Drawing.Color]::FromArgb(255, 0xFF, 0xB3, 0x00)
$bottom = [System.Drawing.Color]::FromArgb(255, 0xFF, 0x3D, 0x00)
$white  = [System.Drawing.Color]::White

function New-IconBitmap([int]$px) {
    $bmp = New-Object System.Drawing.Bitmap($px, $px, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = $px / 100.0

    # Hexagon with the Blazma amber-to-orange gradient.
    $hex = @(@(50, 3), @(91, 26.5), @(91, 73.5), @(50, 97), @(9, 73.5), @(9, 26.5))
    $poly = @()
    foreach ($p in $hex) {
        $poly += New-Object System.Drawing.PointF(($p[0] * $s), ($p[1] * $s))
    }
    $rect = New-Object System.Drawing.RectangleF(0, 0, $px, $px)
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $top, $bottom, 90.0)
    $g.FillPolygon($grad, [System.Drawing.PointF[]]$poly)

    # Crosshair: ring, four arms and a centre dot.
    $pen = New-Object System.Drawing.Pen($white, (6.0 * $s))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawEllipse($pen, (31 * $s), (31 * $s), (38 * $s), (38 * $s))
    $g.DrawLine($pen, (50 * $s), (21 * $s), (50 * $s), (37 * $s))
    $g.DrawLine($pen, (50 * $s), (63 * $s), (50 * $s), (79 * $s))
    $g.DrawLine($pen, (21 * $s), (50 * $s), (37 * $s), (50 * $s))
    $g.DrawLine($pen, (63 * $s), (50 * $s), (79 * $s), (50 * $s))
    $brushWhite = New-Object System.Drawing.SolidBrush($white)
    $g.FillEllipse($brushWhite, (45.5 * $s), (45.5 * $s), (9 * $s), (9 * $s))

    $grad.Dispose(); $pen.Dispose(); $brushWhite.Dispose(); $g.Dispose()
    return $bmp
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($s in $sizes) {
    $b = New-IconBitmap $s
    $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , $ms.ToArray()
    $b.Dispose(); $ms.Dispose()
}

# Reference PNG for eyeballing the shape.
$big = New-IconBitmap 512
$big.Save((Join-Path $OutDir 'blazma-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$big.Dispose()

# ICO container: directory of PNG-compressed entries (Vista+).
$icoPath = Join-Path $OutDir 'blazma.ico'
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0)                 # reserved
$bw.Write([uint16]1)                 # type: icon
$bw.Write([uint16]$sizes.Count)

$offset = 6 + (16 * $sizes.Count)
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dim = $sizes[$i]
    $bw.Write([byte]($(if ($dim -ge 256) { 0 } else { $dim })))
    $bw.Write([byte]($(if ($dim -ge 256) { 0 } else { $dim })))
    $bw.Write([byte]0)               # palette count
    $bw.Write([byte]0)               # reserved
    $bw.Write([uint16]1)             # colour planes
    $bw.Write([uint16]32)            # bits per pixel
    $bw.Write([uint32]$pngs[$i].Length)
    $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Flush(); $bw.Close(); $fs.Close()

"icon written: $icoPath ({0:N0} bytes)" -f (Get-Item $icoPath).Length
