# Generates the Blazma app icon (rounded square + lightning bolt) as a multi-size .ico.
param([string]$OutDir)

Add-Type -AssemblyName System.Drawing

$bg   = [System.Drawing.Color]::FromArgb(255, 0x15, 0x13, 0x1A)
$bolt = [System.Drawing.Color]::FromArgb(255, 0x8B, 0x7F, 0xE8)

function New-IconBitmap([int]$px) {
    $bmp = New-Object System.Drawing.Bitmap($px, $px, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = $px / 1024.0

    # Rounded square backdrop.
    $r = 232.0 * $s
    $w = 1024.0 * $s
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($w - $d, 0, $d, $d, 270, 90)
    $path.AddArc($w - $d, $w - $d, $d, $d, 0, 90)
    $path.AddArc(0, $w - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $brushBg = New-Object System.Drawing.SolidBrush($bg)
    $g.FillPath($brushBg, $path)

    # Lightning bolt.
    $pts = @(
        @(602, 186), @(352, 540), @(500, 540),
        @(424, 832), @(672, 478), @(520, 478)
    )
    $poly = @()
    foreach ($p in $pts) {
        $poly += New-Object System.Drawing.PointF(($p[0] * $s), ($p[1] * $s))
    }
    $brushBolt = New-Object System.Drawing.SolidBrush($bolt)
    $g.FillPolygon($brushBolt, [System.Drawing.PointF[]]$poly)

    $brushBg.Dispose(); $brushBolt.Dispose(); $path.Dispose(); $g.Dispose()
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
