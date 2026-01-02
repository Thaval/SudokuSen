namespace SudokuSen.UI;

using SudokuSen.Services;

public static class MiniGridRenderer
{
    public static Control CreateMiniGridWithLegends(
        int[,] values,
        bool[,] isGiven,
        HashSet<(int row, int col)> highlightedCells,
        HashSet<(int row, int col)> relatedCells,
        ThemeService theme,
        ThemeService.ThemeColors colors,
        (int row, int col, int value)? solutionCell = null,
        Dictionary<(int row, int col), int[]>? candidates = null,
        int cellSize = 34)
    {
        int rows = values.GetLength(0);
        int cols = values.GetLength(1);

        if (rows <= 0 || cols <= 0)
        {
            return new Label { Text = "" };
        }

        if (isGiven.GetLength(0) != rows || isGiven.GetLength(1) != cols)
        {
            return new Label { Text = LocalizationService.Instance.Get("minigrid.invalid_data") };
        }

        int gridSize = Math.Max(rows, cols);
        int blockSize = GetBlockSize(gridSize);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 4);

        // Column labels row
        var topRow = new HBoxContainer();
        topRow.AddThemeConstantOverride("separation", 4);
        root.AddChild(topRow);

        var corner = new Control();
        corner.CustomMinimumSize = new Vector2(24, 20);
        topRow.AddChild(corner);

        var colLabels = new HBoxContainer();
        colLabels.AddThemeConstantOverride("separation", 0);
        topRow.AddChild(colLabels);

        // Calculate actual cell widths including borders for each column
        for (int c = 0; c < cols; c++)
        {
            bool isRightBlockBorder = (c + 1) % blockSize == 0 && c < cols - 1;
            bool isLeftBlockBorder = c % blockSize == 0 && c > 0;

            float actualCellWidth = cellSize;
            // Add right margin (content margin)
            actualCellWidth += isRightBlockBorder ? 3 : 1;
            // Add left margin (content margin)
            actualCellWidth += isLeftBlockBorder ? 3 : 1;
            // Add border widths if present
            if (isRightBlockBorder) actualCellWidth += 3;
            if (isLeftBlockBorder) actualCellWidth += 3;

            var label = new Label();
            label.Text = GetColumnName(c);
            label.CustomMinimumSize = new Vector2(actualCellWidth, 20);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 12);
            label.AddThemeColorOverride("font_color", colors.TextSecondary);
            colLabels.AddChild(label);
        }

        // Main row: row labels + grid
        var mainRow = new HBoxContainer();
        mainRow.AddThemeConstantOverride("separation", 4);
        root.AddChild(mainRow);

        var rowLabels = new VBoxContainer();
        rowLabels.AddThemeConstantOverride("separation", 0);
        mainRow.AddChild(rowLabels);

        // Calculate actual cell heights including borders for each row
        for (int r = 0; r < rows; r++)
        {
            bool isBottomBlockBorder = (r + 1) % blockSize == 0 && r < rows - 1;
            bool isTopBlockBorder = r % blockSize == 0 && r > 0;

            float actualCellHeight = cellSize;
            // Add bottom margin (content margin)
            actualCellHeight += isBottomBlockBorder ? 3 : 1;
            // Add top margin (content margin)
            actualCellHeight += isTopBlockBorder ? 3 : 1;
            // Add border widths if present
            if (isBottomBlockBorder) actualCellHeight += 3;
            if (isTopBlockBorder) actualCellHeight += 3;

            var label = new Label();
            label.Text = (r + 1).ToString();
            label.CustomMinimumSize = new Vector2(24, actualCellHeight);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 12);
            label.AddThemeColorOverride("font_color", colors.TextSecondary);
            rowLabels.AddChild(label);
        }

        var gridPanel = new PanelContainer();
        var gridStyle = new StyleBoxFlat();
        gridStyle.BgColor = colors.GridLineThick;
        gridStyle.ContentMarginLeft = 4;
        gridStyle.ContentMarginRight = 4;
        gridStyle.ContentMarginTop = 4;
        gridStyle.ContentMarginBottom = 4;
        gridStyle.CornerRadiusTopLeft = 8;
        gridStyle.CornerRadiusTopRight = 8;
        gridStyle.CornerRadiusBottomLeft = 8;
        gridStyle.CornerRadiusBottomRight = 8;
        gridPanel.AddThemeStyleboxOverride("panel", gridStyle);
        mainRow.AddChild(gridPanel);

        var grid = new GridContainer();
        grid.Columns = cols;
        gridPanel.AddChild(grid);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int value = values[r, c];
                bool given = isGiven[r, c];
                bool isHighlighted = highlightedCells.Contains((r, c));
                bool isRelated = relatedCells.Contains((r, c));

                bool isSolutionCell = solutionCell.HasValue && solutionCell.Value.row == r && solutionCell.Value.col == c;

                var cellPanel = new PanelContainer();
                cellPanel.CustomMinimumSize = new Vector2(cellSize, cellSize);

                Color bgColor;
                if (isHighlighted)
                    bgColor = colors.CellBackgroundSelected;
                else if (isRelated)
                    bgColor = new Color("ffb74d").Lerp(colors.CellBackground, 0.4f);
                else if (given)
                    bgColor = colors.CellBackgroundGiven;
                else
                    bgColor = colors.CellBackground;

                var cellStyle = new StyleBoxFlat();
                cellStyle.BgColor = bgColor;

                bool isRightBlockBorder = (c + 1) % blockSize == 0 && c < cols - 1;
                bool isBottomBlockBorder = (r + 1) % blockSize == 0 && r < rows - 1;
                bool isLeftBlockBorder = c % blockSize == 0 && c > 0;
                bool isTopBlockBorder = r % blockSize == 0 && r > 0;

                cellStyle.ContentMarginRight = isRightBlockBorder ? 3 : 1;
                cellStyle.ContentMarginBottom = isBottomBlockBorder ? 3 : 1;
                cellStyle.ContentMarginLeft = isLeftBlockBorder ? 3 : 1;
                cellStyle.ContentMarginTop = isTopBlockBorder ? 3 : 1;

                if (isRightBlockBorder)
                {
                    cellStyle.BorderWidthRight = 3;
                    cellStyle.BorderColor = colors.GridLineThick;
                }
                if (isBottomBlockBorder)
                {
                    cellStyle.BorderWidthBottom = 3;
                    cellStyle.BorderColor = colors.GridLineThick;
                }
                if (isLeftBlockBorder)
                {
                    cellStyle.BorderWidthLeft = 3;
                    cellStyle.BorderColor = colors.GridLineThick;
                }
                if (isTopBlockBorder)
                {
                    cellStyle.BorderWidthTop = 3;
                    cellStyle.BorderColor = colors.GridLineThick;
                }

                if (isHighlighted)
                {
                    cellStyle.BorderColor = colors.Accent;
                    cellStyle.BorderWidthLeft = 3;
                    cellStyle.BorderWidthRight = 3;
                    cellStyle.BorderWidthTop = 3;
                    cellStyle.BorderWidthBottom = 3;
                }

                cellPanel.AddThemeStyleboxOverride("panel", cellStyle);

                int[]? cellCandidates = null;
                bool hasCandidates = value == 0 && candidates != null && candidates.TryGetValue((r, c), out cellCandidates);

                if (hasCandidates && cellCandidates != null)
                {
                    int notesColumns = gridSize == 4 ? 2 : 3;
                    var candidateGrid = new GridContainer();
                    candidateGrid.Columns = notesColumns;
                    candidateGrid.SetAnchorsPreset(Control.LayoutPreset.FullRect);

                    var candidateSet = new HashSet<int>(cellCandidates);
                    for (int i = 1; i <= gridSize; i++)
                    {
                        var candLabel = new Label();
                        candLabel.Text = candidateSet.Contains(i) ? i.ToString() : "";
                        candLabel.HorizontalAlignment = HorizontalAlignment.Center;
                        candLabel.VerticalAlignment = VerticalAlignment.Center;
                        candLabel.AddThemeFontSizeOverride("font_size", 8);
                        candLabel.AddThemeColorOverride("font_color", colors.TextUser);
                        candLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
                        candLabel.SizeFlagsVertical = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
                        candidateGrid.AddChild(candLabel);
                    }

                    cellPanel.AddChild(candidateGrid);
                }
                else
                {
                    var label = new Label();
                    label.HorizontalAlignment = HorizontalAlignment.Center;
                    label.VerticalAlignment = VerticalAlignment.Center;
                    label.AddThemeFontSizeOverride("font_size", 16);

                    int displayValue = value;
                    if (isSolutionCell && value == 0)
                    {
                        displayValue = solutionCell!.Value.value;
                    }

                    if (displayValue > 0)
                    {
                        label.Text = displayValue.ToString();

                        if (isSolutionCell && value == 0)
                            label.AddThemeColorOverride("font_color", new Color("4caf50"));
                        else if (given)
                            label.AddThemeColorOverride("font_color", colors.TextGiven);
                        else
                            label.AddThemeColorOverride("font_color", colors.TextUser);
                    }
                    else
                    {
                        label.Text = "";
                    }

                    cellPanel.AddChild(label);
                }

                grid.AddChild(cellPanel);
            }
        }

        return root;
    }

    private static int GetBlockSize(int gridSize)
    {
        if (gridSize == 9) return 3;
        if (gridSize == 4) return 2;

        int sqrt = (int)Math.Round(Math.Sqrt(gridSize));
        if (sqrt * sqrt == gridSize) return sqrt;

        return gridSize;
    }

    private static string GetColumnName(int col)
    {
        int idx = col % 26;
        return ((char)('A' + idx)).ToString();
    }
}
