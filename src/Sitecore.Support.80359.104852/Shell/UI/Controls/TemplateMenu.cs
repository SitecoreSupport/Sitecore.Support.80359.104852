using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Form.Core.Configuration;
using Sitecore.WFFM.Abstractions.Dependencies;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.UI;

namespace Sitecore.Support.Forms.Shell.UI.Controls
{
  public class TemplateMenu : Sitecore.Web.UI.HtmlControls.Control
  {
    public EventHandler Change;

    private List<Item> standardFields;

    private List<string> standardSections;

    public string TemplateID
    {
      get
      {
        return base.GetViewStateString("templateID");
      }
      set
      {
        base.SetViewStateString("templateID", value);
      }
    }

    public string ShowStandardField
    {
      get
      {
        return base.GetViewStateString("StandardField");
      }
      set
      {
        base.SetViewStateString("StandardField", value);
      }
    }

    public string TemplateFieldName
    {
      get
      {
        return base.GetViewStateString("templatefield");
      }
      set
      {
        base.SetViewStateString("templatefield", value);
      }
    }

    public string TemplateFieldID
    {
      get
      {
        return base.GetViewStateString("templatefieldid");
      }
      set
      {
        base.SetViewStateString("templatefieldid", value);
      }
    }

    public string FieldName
    {
      get
      {
        return base.GetViewStateString("field");
      }
      set
      {
        base.SetViewStateString("field", value);
      }
    }

    public string FieldID
    {
      get
      {
        return base.GetViewStateString("fieldid");
      }
      set
      {
        base.SetViewStateString("fieldid", value);
      }
    }

    public TemplateMenu()
    {
    }

    public TemplateMenu(string template) : this()
    {
      this.TemplateID = template;
      base.Attributes["class"] = "scfEntry";
    }

    private void ChangeTemplateField(string value)
    {
      this.TemplateFieldID = value;
    }

    protected override void DoRender(HtmlTextWriter output)
    {
      output.Write("<div" + base.ControlAttributes + ">");
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.AppendFormat("<span class=\"scfFieldName\">{0}:</span>", this.FieldName);
      stringBuilder.AppendFormat("<select class=\"scfAborder\" id='select_{0}'", this.ID);
      stringBuilder.AppendFormat(" onchange=\"scForm.postEvent(this,event,'{0}.ChangeTemplateField(&quot;' + this.value + '&quot;)')\" >", this.ID);
      stringBuilder.Append("<option class='scfNotDefined'");
      if (this.TemplateFieldID == null || this.TemplateFieldID == Sitecore.Data.ID.Null.ToString())
      {
        stringBuilder.Append(" selected='selected'");
      }
      stringBuilder.AppendFormat("value='{0}'>{1}</option>", Sitecore.Data.ID.Null, DependenciesManager.ResourceManager.Localize("NOD_DEFINED"));
      output.Write(stringBuilder.ToString());
      if (!string.IsNullOrEmpty(this.TemplateID))
      {
        this.TemplateContent(StaticSettings.ContextDatabase.GetTemplate(this.TemplateID), output);
      }
      output.Write("</select>");
      output.Write("</div>");
    }

    internal void Redraw()
    {
      StringWriter writer = new StringWriter(new StringBuilder());
      HtmlTextWriter htmlTextWriter = new HtmlTextWriter(writer);
      this.DoRender(htmlTextWriter);
      SheerResponse.SetOuterHtml(this.ID, htmlTextWriter.InnerWriter.ToString());
    }

    private void RenderTemplates(TemplateItem template, HtmlTextWriter writer)
    {
      if (template != null)
      {
        TemplateItem[] baseTemplates = template.BaseTemplates;
        for (int i = 0; i < baseTemplates.Length; i++)
        {
          TemplateItem templateItem = baseTemplates[i];
          if (templateItem.ID != TemplateIDs.StandardTemplate)
          {
            this.RenderTemplatePart(templateItem, writer);
            this.RenderTemplates(templateItem, writer);
          }
        }
      }
    }

    private void TemplateContent(TemplateItem template, HtmlTextWriter writer)
    {
      if (template != null)
      {
        this.standardFields = new List<Item>();
        this.standardSections = new List<string>();
        this.RenderTemplatePart(template, writer);
        this.RenderTemplates(template, writer);
        if (this.ShowStandardField == "1")
        {
          this.RenderStandardFields(template, writer);
        }
      }
    }

    private void RenderTemplatePart(TemplateItem template, HtmlTextWriter writer)
    {
      TemplateSectionItem[] sections = template.GetSections();
      for (int i = 0; i < sections.Length; i++)
      {
        TemplateSectionItem templateSectionItem = sections[i];
        writer.Write("<optgroup  class=\"scEditorHeaderNavigatorSection\" label=\"" + templateSectionItem.DisplayName + "\">");
        TemplateFieldItem[] fields = templateSectionItem.GetFields();
        for (int j = 0; j < fields.Length; j++)
        {
          TemplateFieldItem templateFieldItem = fields[j];
          string text = templateFieldItem.ID.ToString();
          writer.Write(string.Concat(new string[]
          {
                        "<option id=\"",
                        text,
                        "\" value=\"",
                        text,
                        "\""
          }));
          writer.Write(" class=\"scEditorHeaderNavigatorField\" ");
          if (text == this.TemplateFieldID)
          {
            writer.Write(" selected=\"selected\"");
          }
          writer.Write(">" + templateFieldItem.DisplayName + "</option>");
        }
        writer.Write("</optgroup>");
      }
    }


    private void RenderStandardFields(TemplateItem template, HtmlTextWriter writer)
    {
      if (template != null)
      {
        Database database = Factory.GetDatabase("master");
        TemplateItem templateItem = new TemplateItem(database.GetItem(TemplateIDs.StandardTemplate));
        if (templateItem != null)
        {
          TemplateItem[] baseTemplates = templateItem.BaseTemplates;
          for (int i = 0; i < baseTemplates.Length; i++)
          {
            TemplateItem template2 = baseTemplates[i];
            this.RenderTemplatePart(template2, writer);
            this.RenderTemplates(template2, writer);
          }
        }
      }
    }
  }
}
