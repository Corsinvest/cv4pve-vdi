/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using AGrid = Avalonia.Controls.Grid;

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

internal static class GridExtensions
{
    public static void Add(this AGrid grid, Control control, int col, int row = 0)
    {
        AGrid.SetColumn(control, col);
        AGrid.SetRow(control, row);
        grid.Children.Add(control);
    }
}
