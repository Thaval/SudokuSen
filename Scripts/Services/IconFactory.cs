using Godot;
using System;

namespace MySudoku.Services;

/// <summary>
/// Factory für programmtisch erstellte Icons
/// </summary>
public static class IconFactory
{
    private const int ICON_SIZE = 64;

    /// <summary>
    /// Erstellt ein Play-Icon (Dreieck)
    /// </summary>
    public static ImageTexture CreatePlayIcon(Color color)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        // Dreieck zeichnen
        int padding = 16;
        for (int y = padding; y < ICON_SIZE - padding; y++)
        {
            int relY = y - padding;
            int height = ICON_SIZE - 2 * padding;
            int width = (int)((float)relY / height * (ICON_SIZE - 2 * padding));
            int startX = padding;
            int endX = padding + width;

            for (int x = startX; x <= endX; x++)
            {
                image.SetPixel(x, y, color);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Erstellt ein Continue-Icon (zwei vertikale Balken + Dreieck)
    /// </summary>
    public static ImageTexture CreateContinueIcon(Color color)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        // Dreieck
        int padding = 18;
        for (int y = padding; y < ICON_SIZE - padding; y++)
        {
            int relY = y - padding;
            int height = ICON_SIZE - 2 * padding;
            int width = (int)((float)relY / height * (ICON_SIZE / 2 - padding));
            int startX = ICON_SIZE / 3;
            int endX = startX + width;

            for (int x = startX; x <= endX && x < ICON_SIZE; x++)
            {
                image.SetPixel(x, y, color);
            }
        }

        // Balken links
        for (int y = padding; y < ICON_SIZE - padding; y++)
        {
            for (int x = 8; x < 16; x++)
            {
                image.SetPixel(x, y, color);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Erstellt ein Settings-Icon (Zahnrad)
    /// </summary>
    public static ImageTexture CreateSettingsIcon(Color color)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        int centerX = ICON_SIZE / 2;
        int centerY = ICON_SIZE / 2;
        int outerRadius = 24;
        int innerRadius = 10;

        // Äußerer Kreis
        for (int y = 0; y < ICON_SIZE; y++)
        {
            for (int x = 0; x < ICON_SIZE; x++)
            {
                float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                if (dist <= outerRadius && dist >= outerRadius - 6)
                {
                    image.SetPixel(x, y, color);
                }
                // Innerer Kreis (Loch)
                if (dist <= innerRadius)
                {
                    image.SetPixel(x, y, Colors.Transparent);
                }
            }
        }

        // Zähne (vereinfacht: 8 Rechtecke)
        int toothSize = 8;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.Pi / 4;
            int toothX = (int)(centerX + Mathf.Cos(angle) * (outerRadius - 2));
            int toothY = (int)(centerY + Mathf.Sin(angle) * (outerRadius - 2));

            for (int dy = -toothSize / 2; dy <= toothSize / 2; dy++)
            {
                for (int dx = -toothSize / 2; dx <= toothSize / 2; dx++)
                {
                    int px = toothX + dx;
                    int py = toothY + dy;
                    if (px >= 0 && px < ICON_SIZE && py >= 0 && py < ICON_SIZE)
                    {
                        image.SetPixel(px, py, color);
                    }
                }
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Erstellt ein History-Icon (Uhr)
    /// </summary>
    public static ImageTexture CreateHistoryIcon(Color color)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        int centerX = ICON_SIZE / 2;
        int centerY = ICON_SIZE / 2;
        int radius = 26;

        // Kreisumriss
        for (int y = 0; y < ICON_SIZE; y++)
        {
            for (int x = 0; x < ICON_SIZE; x++)
            {
                float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                if (dist <= radius && dist >= radius - 4)
                {
                    image.SetPixel(x, y, color);
                }
            }
        }

        // Zeiger
        for (int i = 0; i < 16; i++)
        {
            // Stundenzeiger
            image.SetPixel(centerX, centerY - i, color);
            // Minutenzeiger
            image.SetPixel(centerX + i, centerY, color);
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Erstellt ein Stats-Icon (Balkendiagramm)
    /// </summary>
    public static ImageTexture CreateStatsIcon(Color color)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        int barWidth = 12;
        int spacing = 4;
        int[] heights = { 24, 40, 32, 48 };

        for (int i = 0; i < 4; i++)
        {
            int x = 8 + i * (barWidth + spacing);
            int h = heights[i];
            int y = ICON_SIZE - 8 - h;

            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < barWidth; dx++)
                {
                    image.SetPixel(x + dx, y + dy, color);
                }
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Erstellt ein Exit-Icon (X)
    /// </summary>
    public static ImageTexture CreateExitIcon(Color color)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        int padding = 12;
        int thickness = 4;

        for (int i = 0; i < ICON_SIZE - 2 * padding; i++)
        {
            for (int t = 0; t < thickness; t++)
            {
                // Linie von links oben nach rechts unten
                int x1 = padding + i;
                int y1 = padding + i + t;
                if (y1 < ICON_SIZE - padding)
                    image.SetPixel(x1, y1, color);

                // Linie von rechts oben nach links unten
                int x2 = ICON_SIZE - padding - i - 1;
                int y2 = padding + i + t;
                if (y2 < ICON_SIZE - padding)
                    image.SetPixel(x2, y2, color);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Erstellt ein Eraser-Icon
    /// </summary>
    public static ImageTexture CreateEraserIcon(Color color)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        // Radiergummi-Form (Rechteck mit abgerundeter Spitze)
        int padding = 10;

        for (int y = padding; y < ICON_SIZE - padding; y++)
        {
            for (int x = padding; x < ICON_SIZE - padding; x++)
            {
                // Rechteck
                if (y >= padding + 10)
                {
                    image.SetPixel(x, y, color);
                }
                // Abgerundete Spitze oben
                else
                {
                    float dist = Mathf.Abs(x - ICON_SIZE / 2);
                    if (dist < 18 - (padding + 10 - y))
                    {
                        image.SetPixel(x, y, color);
                    }
                }
            }
        }

        // Linie für Trennung
        for (int x = padding + 2; x < ICON_SIZE - padding - 2; x++)
        {
            image.SetPixel(x, ICON_SIZE - padding - 15, Colors.White);
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Erstellt ein Tips-Icon (Glühbirne)
    /// </summary>
    public static ImageTexture CreateTipsIcon(Color color)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        int centerX = ICON_SIZE / 2;
        int bulbRadius = 18;

        // Glühbirnen-Kopf (Kreis)
        for (int y = 8; y < 40; y++)
        {
            for (int x = 0; x < ICON_SIZE; x++)
            {
                float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - 22) * (y - 22));
                if (dist <= bulbRadius && dist >= bulbRadius - 4)
                {
                    image.SetPixel(x, y, color);
                }
            }
        }

        // Sockel
        for (int y = 40; y < 52; y++)
        {
            for (int x = centerX - 10; x <= centerX + 10; x++)
            {
                if ((y - 40) % 4 < 2) // Streifen
                    image.SetPixel(x, y, color);
            }
        }

        // Kontakt unten
        for (int x = centerX - 6; x <= centerX + 6; x++)
        {
            image.SetPixel(x, 54, color);
            image.SetPixel(x, 55, color);
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Erstellt ein Zahlen-Icon (1-9)
    /// </summary>
    public static ImageTexture CreateNumberIcon(int number, Color bgColor, Color textColor)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(bgColor);

        // Einfache Darstellung - Pixel-Zahlen wären zu komplex
        // Stattdessen ein einfaches Muster

        // Rahmen
        for (int x = 4; x < ICON_SIZE - 4; x++)
        {
            image.SetPixel(x, 4, textColor);
            image.SetPixel(x, ICON_SIZE - 5, textColor);
        }
        for (int y = 4; y < ICON_SIZE - 4; y++)
        {
            image.SetPixel(4, y, textColor);
            image.SetPixel(ICON_SIZE - 5, y, textColor);
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Erstellt ein Back/Zurück-Icon (Pfeil nach links)
    /// </summary>
    public static ImageTexture CreateBackIcon(Color color)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        int centerY = ICON_SIZE / 2;

        // Pfeilspitze
        for (int i = 0; i < 20; i++)
        {
            for (int t = -2; t <= 2; t++)
            {
                image.SetPixel(12 + i, centerY - i + t, color);
                image.SetPixel(12 + i, centerY + i + t, color);
            }
        }

        // Schaft
        for (int x = 20; x < 52; x++)
        {
            for (int t = -3; t <= 3; t++)
            {
                image.SetPixel(x, centerY + t, color);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Erstellt ein Forward/Weiter-Icon (Pfeil nach rechts)
    /// </summary>
    public static ImageTexture CreateForwardIcon(Color color)
    {
        var image = Image.CreateEmpty(ICON_SIZE, ICON_SIZE, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        int centerY = ICON_SIZE / 2;

        // Pfeilspitze
        for (int i = 0; i < 20; i++)
        {
            for (int t = -2; t <= 2; t++)
            {
                image.SetPixel(52 - i, centerY - i + t, color);
                image.SetPixel(52 - i, centerY + i + t, color);
            }
        }

        // Schaft
        for (int x = 12; x < 44; x++)
        {
            for (int t = -3; t <= 3; t++)
            {
                image.SetPixel(x, centerY + t, color);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }
}
