using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using InteriorElevationsByRoom.Extensions;

namespace InteriorElevationsByRoom.Methods
{
    internal class Methods
    {
        public static void CreateElevations(Document doc, List<Room> rooms, string viewNamingFormat)
        {
            // Get the view family type
            ViewFamilyType viewFamilyType
                = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault<ViewFamilyType>(x => ViewFamily.Elevation == x.ViewFamily);

            if (viewFamilyType == null)
            {
                TaskDialog.Show("Error", "No view family type found");
                return;
            }

            foreach (Room room in rooms)
            {
                //if (room.Floors().Any())
                //{
                //    using (Transaction t = new Transaction(doc,"move floors temp"))
                //    {
                //        t.Start();
                //        foreach (var floor in room.Floors())
                //        {
                //            floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(-0.003);
                //        }
                //        t.Commit();
                //    }
                   
                //}
                
                // Get the placement point of the elevation marker, whcih is the center of the room

                BoundingBoxXYZ bBox = room.get_BoundingBox(doc.ActiveView);
                Level roomLevel = doc.GetElement(room.LevelId) as Level;
                double roomElevation = roomLevel.Elevation;
                XYZ min = new XYZ(bBox.Min.X, bBox.Min.Y, roomElevation);
                XYZ max = new XYZ(bBox.Max.X, bBox.Max.Y, roomElevation + bBox.Max.Z - bBox.Min.Z);
                XYZ placementPoint = (min + max) / 2;

                ElevationMarker marker = null;
                ViewSection v1 = null;
                ViewSection v2 = null;
                ViewSection v3 = null;
                ViewSection v4 = null;


                using (Transaction transaction = new Transaction(doc, "room elevations"))
                {
                    if (doc.IsModifiable == false) transaction.Start();
                    
                    marker = ElevationMarker.CreateElevationMarker(doc, viewFamilyType.Id, placementPoint, 50);

                    if (transaction.HasStarted()) transaction.Commit();
                }

                using (Transaction transaction = new Transaction(doc, "create placeholder"))
                {
                    if (doc.IsModifiable == false) transaction.Start();

                    //Create Elevations
                    v1 = marker.CreateElevation(doc, doc.ActiveView.Id, 0);
                    v2 = marker.CreateElevation(doc, doc.ActiveView.Id, 1);
                    v3 = marker.CreateElevation(doc, doc.ActiveView.Id, 2);
                    v4 = marker.CreateElevation(doc, doc.ActiveView.Id, 3);

                    // Rename the views as per the format
                    v1.Name = viewNamingFormat.Replace("%roomname%", room.Name).Replace("#", "1");
                    v2.Name = viewNamingFormat.Replace("%roomname%", room.Name).Replace("#", "2");
                    v3.Name = viewNamingFormat.Replace("%roomname%", room.Name).Replace("#", "3");
                    v4.Name = viewNamingFormat.Replace("%roomname%", room.Name).Replace("#", "4");

                    // Apply the view template
                    //v1.ViewTemplateId = viewTemplate.Id;
                    //v2.ViewTemplateId = viewTemplate.Id;
                    //v3.ViewTemplateId = viewTemplate.Id;
                    //v4.ViewTemplateId = viewTemplate.Id;

                    
                    if (transaction.HasStarted()) transaction.Commit();
                }

              
                // The elevation marker to face the longest wall of the room
                Curve longestBoundary = null;
                double maxLength = 0;
                List<Curve> curves = new List<Curve>();
                SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
                options.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;
                foreach (IList<BoundarySegment> bsLoop in room.GetBoundarySegments(options))
                {
                    foreach (BoundarySegment bs in bsLoop)
                    {
                        Curve c = bs.GetCurve();
                        if (c.Length > maxLength)
                        {
                            maxLength = c.Length;
                            longestBoundary = c;
                        }
                    }
                }

                XYZ longestCurveDirection = (longestBoundary.GetEndPoint(1) - longestBoundary.GetEndPoint(0)).Normalize();
                double angleOfCurveDirection = Math.Atan2(longestCurveDirection.Y, longestCurveDirection.X);
                double angleToRotate = angleOfCurveDirection - Math.PI / 2;

                // Rotate the marker to align with the longest boundary, facing the longest wall
                using (Transaction transaction = new Transaction(doc, "rotate"))
                {
                    if (doc.IsModifiable == false) transaction.Start();

                    ElementTransformUtils.RotateElement(
                        doc,
                        marker.Id,
                        Line.CreateBound(placementPoint, placementPoint + XYZ.BasisZ),
                        angleToRotate
                    );
                    if (transaction.HasStarted()) transaction.Commit();
                }



                using (Transaction transaction = new Transaction(doc, "name"))
                {
                    if (doc.IsModifiable == false) transaction.Start();

                    //// Adjust the crop box (Tried this o adjust crop boundary, but it is not changing the height of the crop view, 
                    ///but it is extending the crop window width to the whole extents of the model)
                    AdjustCropBox(v1, room);
                    AdjustCropBox(v2, room);
                    AdjustCropBox(v3, room);
                    AdjustCropBox(v4, room);

                    transaction.Commit();
                }

            }
        }

        private static void AdjustCropBox(ViewSection view, Room room)
        {
            // Get the crop box
            BoundingBoxXYZ cropBox = view.CropBox;
            // Get the room's bounding box
            // INSTEAD OF USING BOUNDING BOX DIRECTLY WITH NO VIEW, GET THE CLOSED SHELL FIRST. - JOHNP

            BoundingBoxXYZ roomBox = room.get_BoundingBox(view);

            // Calculate the height of the room
            //double roomHeight = roomBox.Max.Z - roomBox.Min.Z;

            // Adjust the top and bottom of the crop box
            // We'll add 2 feet extra at the top and bottom for good measure
            var min = new XYZ(cropBox.Min.X, cropBox.Min.Y, roomBox.Min.Z - 2);
            var max = new XYZ(cropBox.Max.X, cropBox.Max.Y, roomBox.Max.Z + 2);

            // Apply the adjusted crop box
            var newBox = new BoundingBoxXYZ();
            newBox.Min = min;
            newBox.Max = max;

            view.CropBox = newBox;

            // Ensure the crop view is active
            view.CropBoxActive = true;
            view.CropBoxVisible = true;
        }
    }
}
