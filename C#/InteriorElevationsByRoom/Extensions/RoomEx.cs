using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Architecture;

namespace InteriorElevationsByRoom.Extensions
{
    internal static class RoomEx
    {
        internal static List<Floor> Floors(this Room room)
        {
            var doc = room.Document;

            var roomBoundingBox = room.ClosedShell.GetBoundingBox();
            Outline roomOutline = new Outline(roomBoundingBox.Min, roomBoundingBox.Max);

            BoundingBoxIntersectsFilter boundingBoxFilter = new BoundingBoxIntersectsFilter(roomOutline);

            var floors = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Floors).WherePasses(boundingBoxFilter).Cast<Floor>().ToList();

            if (floors.Any())
            {
                return floors;
            }
            else
            {
                return null;
            }
        }
    }
}
