using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Nice3point.Revit.Toolkit.External;
using Nice3point.Revit.Toolkit.Options;

namespace InteriorElevationsByRoom.Commands
{
    /// <summary>
    ///     External command entry point invoked from the Revit interface
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class StartupCommand : ExternalCommand
    {
        public override void Execute()
        {
            var selectionConfiguration = new SelectionConfiguration()
                .Allow.Element(element => element.Category.Id.AreEquals(BuiltInCategory.OST_Rooms))
                .Allow.Reference((reference, xyz) => false);

            var selection = UiDocument.Selection.PickObjects(ObjectType.Element, selectionConfiguration.Filter);

            if(!selection.Any()) return;

            var rooms  = selection.Select(s => Document.GetElement(s.ElementId) as Autodesk.Revit.DB.Architecture.Room).ToList();


            Methods.Methods.CreateElevations(Document,rooms, "%roomname%#");
        } 

    }

}