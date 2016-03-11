using System;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Events;
using Sitecore.Globalization;
using Version = Sitecore.Data.Version;
using Sitecore.SecurityModel;

namespace TemplateLayoutDeltas.Web.Tasks
{
    public class TemplateLayoutDeltasReconciler
    {
        public void OnItemSaving(object sender, EventArgs args)
        {
            Item item = Event.ExtractParameter(args, 0) as Item;
            PropagateLayoutChanges(item);
        }

        private void PropagateLayoutChanges(Item item)
        {
            if (StandardValuesManager.IsStandardValuesHolder(item))
            {
                Item oldItem = item.Database.GetItem(item.ID, item.Language, item.Version);
                string layout = item[FieldIDs.LayoutField];
                string oldLayout = oldItem[FieldIDs.LayoutField];
                if (layout != oldLayout)
                {
                    string delta = XmlDeltas.GetDelta(layout, oldLayout);
                    foreach (Template templ in TemplateManager.GetTemplate(item).GetDescendants())
                    {
                        ApplyDeltaToStandardValues(templ, delta, item.Language, item.Version, item.Database);
                    }
                }
            }
        }

        private void ApplyDeltaToStandardValues(Template template, string delta, Language language, Version version, Database database)
        {
            Item item = ItemManager.GetItem(template.StandardValueHolderId, language, version, database, SecurityCheck.Disable);
            Field field = item.Fields[FieldIDs.LayoutField];
            if (!field.ContainsStandardValue)
            {
                string newFieldValue = XmlDeltas.ApplyDelta(field.Value, delta);
                if (newFieldValue != field.Value)
                {
                    using (new EditContext(item))
                    {
                        LayoutField.SetFieldValue(field, newFieldValue);
                    }
                }
            }
        }
    }
}