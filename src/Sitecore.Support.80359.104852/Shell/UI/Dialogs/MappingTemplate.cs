using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Utility;
using Sitecore.Forms.Core.Data;
using Sitecore.Support.Forms.Shell.UI.Controls;
using Sitecore.Shell.Applications.Dialogs.ItemLister;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.WFFM.Abstractions.Data;
using Sitecore.WFFM.Abstractions.Dependencies;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;

namespace Sitecore.Support.Forms.Shell.UI.Dialogs
{
  public class MappingTemplate : WizardForm
  {
    // Fields
    protected Border Collision;
    protected Border CollisionFields;
    protected WizardDialogBaseXmlControl ConfirmationPage;
    protected Literal ConflictLiteral;
    protected Literal DestinationLabel;
    protected Literal DestinationName;
    protected Edit EbDestination;
    protected Edit EbTemplate;
    protected Literal InformationLostLiteral;
    protected Literal ItemsWillBeStoredLiteral;
    protected Border LostFields;
    protected Border MappingBorder;
    protected WizardDialogBaseXmlControl MappingFormPage;
    protected Groupbox MappingGroupbox;
    private NameValueCollection nvParams;
    protected Button SelectDestinationButton;
    protected Button SelectTemplateButton;
    protected WizardDialogBaseXmlControl SelectTemplatePage;
    protected Groupbox SettingsGroupbox;
    protected Checkbox ShowStandardField;
    protected Literal TemplateConfirmLiteral;
    protected Literal TemplateLiteral;
    protected Literal TemplateName;
    protected Border Warning;

    // Methods
    protected override void ActivePageChanged(string page, string oldPage)
    {
      base.ActivePageChanged(page, oldPage);
      if (page == "ConfirmationPage")
      {
        this.RenderLostFieldsWarning();
        this.RenderCollisionFieldsWarning();
        this.TemplateName.Text = this.EbTemplate.Value;
        this.DestinationName.Text = this.EbDestination.Value;
      }
    }

    protected override bool ActivePageChanging(string page, ref string newpage)
    {
      bool flag = base.ActivePageChanging(page, ref newpage);
      if ((page == "SelectTemplatePage") && (newpage == "MappingFormPage"))
      {
        if (this.EbTemplate.Value == string.Empty)
        {
          SheerResponse.Alert(DependenciesManager.ResourceManager.Localize("MESSAGE_SELECT_TEMPLATE"), new string[0]);
          newpage = "SelectTemplatePage";
          return flag;
        }
        if (this.EbDestination.Value == string.Empty)
        {
          SheerResponse.Alert(DependenciesManager.ResourceManager.Localize("MESSAGE_SELECT_DESTINATION"), new string[0]);
          newpage = "SelectTemplatePage";
          return flag;
        }
        this.UpdateMapping();
      }
      return flag;
    }

    private Dictionary<string, List<string>> GetCollisionFields()
    {
      TemplateItem template = StaticSettings.ContextDatabase.GetTemplate(this.EbTemplate.Value);
      Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
      foreach (System.Web.UI.Control control in this.MappingBorder.Controls)
      {
        TemplateMenu menu = control as TemplateMenu;
        if ((((menu != null) && !string.IsNullOrEmpty(menu.TemplateFieldID)) && (menu.TemplateFieldID != ID.Null.ToString())) && ((this.ShowStandardField.Checked && (template.GetField(menu.TemplateFieldID) != null)) || (!this.ShowStandardField.Checked && this.IsOwnField(template, ID.Parse(menu.TemplateFieldID)))))
        {
          if (dictionary.ContainsKey(menu.TemplateFieldID))
          {
            dictionary[menu.TemplateFieldID].Add(menu.FieldName);
          }
          else
          {
            dictionary.Add(menu.TemplateFieldID, new List<string>(new string[] { menu.FieldName }));
          }
        }
      }
      Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>();
      foreach (KeyValuePair<string, List<string>> pair in dictionary)
      {
        if (pair.Value.Count > 1)
        {
          dictionary2.Add(pair.Key, pair.Value);
        }
      }
      return dictionary2;
    }

    private List<string> GetLostFields()
    {
      TemplateItem template = StaticSettings.ContextDatabase.GetTemplate(this.EbTemplate.Value);
      List<string> list = new List<string>();
      foreach (System.Web.UI.Control control in this.MappingBorder.Controls)
      {
        TemplateMenu menu = control as TemplateMenu;
        if ((menu != null) && (((string.IsNullOrEmpty(menu.TemplateFieldID) || (menu.TemplateFieldID == ID.Null.ToString())) || (this.ShowStandardField.Checked && (template.GetField(menu.TemplateFieldID) == null))) || (!this.ShowStandardField.Checked && !this.IsOwnField(template, ID.Parse(menu.TemplateFieldID)))))
        {
          FieldItem item2 = new FieldItem(StaticSettings.ContextDatabase.GetItem(menu.FieldID));
          list.Add(item2.FieldDisplayName);
        }
      }
      return list;
    }

    private string GetMappingResult()
    {
      TemplateItem template = StaticSettings.ContextDatabase.GetTemplate(this.EbTemplate.Value);
      NameValueCollection values = new NameValueCollection();
      foreach (System.Web.UI.Control control in this.MappingBorder.Controls)
      {
        TemplateMenu menu = control as TemplateMenu;
        if ((((menu != null) && !string.IsNullOrEmpty(menu.TemplateFieldID)) && (menu.TemplateFieldID != ID.Null.ToString())) && ((this.ShowStandardField.Checked && (template.GetField(menu.TemplateFieldID) != null)) || (!this.ShowStandardField.Checked && this.IsOwnField(template, ID.Parse(menu.TemplateFieldID)))))
        {
          values.Add(menu.FieldID, menu.TemplateFieldID);
        }
      }
      return StringUtil.NameValuesToString(values, "|");
    }

    public string GetValueByKey(string key)
    {
      if (this.nvParams == null)
      {
        this.nvParams = ParametersUtil.XmlToNameValueCollection(this.Params);
      }
      return MainUtil.DecodeName(this.nvParams[key] ?? string.Empty);
    }

    private bool IsOwnField(TemplateItem template, ID templateFieldID)
    {

      foreach (TemplateFieldItem item in template.OwnFields)
      {
        if (item.ID == templateFieldID)
        {
          return true;
        }
      }
      //the base template fields are not considered -  fix
      foreach (TemplateItem baseTemp in template.BaseTemplates)
      {
        foreach (TemplateFieldItem item in baseTemp.OwnFields)
        {
          if (item.ID == templateFieldID)
          {
            return true;
          }
        }
      }
      return false;
    }

    private void LoadMapping()
    {
      FormItem item = new FormItem(this.CurrentDatabase.GetItem(this.CurrentID));
      IFieldItem[] fields = item.Fields;
      NameValueCollection values = StringUtil.ParseNameValueCollection(this.Mapping, '|', '=');
      foreach (FieldItem item2 in fields)
      {
        string str = item2.ID.ToString();
        TemplateMenu child = new TemplateMenu(this.EbTemplate.Value)
        {
          ID = "t_" + str,
          FieldName = item2.FieldDisplayName,
          FieldID = str,
          TemplateFieldName = DependenciesManager.ResourceManager.Localize("NOD_DEFINED"),
          ShowStandardField = this.ShowStandardField.Checked ? "1" : "0"
        };
        str = str.Replace("-", " ");
        if (!string.IsNullOrEmpty(values[str]))
        {
          child.TemplateFieldID = values[str].Replace(" ", "-");
        }
        this.MappingBorder.Controls.Add(child);
      }
    }

    protected virtual void Localize()
    {
      this.SelectTemplatePage["Header"] = DependenciesManager.ResourceManager.Localize("SELECT_TEMPLATE_CAPTION");
      this.SelectTemplatePage["Text"] = DependenciesManager.ResourceManager.Localize("SELECT_TEMPLATE_AND_DESTINATION_FOR_ITEMS");
      this.TemplateLiteral.Text = DependenciesManager.ResourceManager.Localize("TEMPLATE");
      this.SelectTemplateButton.Header = DependenciesManager.ResourceManager.Localize("BROWSE");
      this.SelectDestinationButton.Header = DependenciesManager.ResourceManager.Localize("BROWSE");
      this.DestinationLabel.Text = DependenciesManager.ResourceManager.Localize("DESTINATION");
      this.MappingFormPage["Header"] = DependenciesManager.ResourceManager.Localize("MAPPING_FORM_FIELDS");
      this.MappingFormPage["Text"] = DependenciesManager.ResourceManager.Localize("MAPPING_FORM_FIELDS_DOT");
      this.ShowStandardField.Header = DependenciesManager.ResourceManager.Localize("SHOW_STANDARD_FIELDS");
      this.MappingGroupbox.Header = DependenciesManager.ResourceManager.Localize("MAPPING");
      this.SettingsGroupbox.Header = DependenciesManager.ResourceManager.Localize("SETTINGS");
      this.ConfirmationPage["Header"] = DependenciesManager.ResourceManager.Localize("CONFIRMATION");
      this.ConfirmationPage["Text"] = DependenciesManager.ResourceManager.Localize("CONFIRM_MAPPING_FORM_TO_ITEM");
      this.TemplateConfirmLiteral.Text = DependenciesManager.ResourceManager.Localize("TEMPLATE_FOR_ITEMS");
      this.ItemsWillBeStoredLiteral.Text = DependenciesManager.ResourceManager.Localize("ITEMS_WILL_BE_STORED");
      this.InformationLostLiteral.Text = DependenciesManager.ResourceManager.Localize("INFORMATION_FROM_FIELDS_WILL_BE_LOST");
      this.ConflictLiteral.Text = DependenciesManager.ResourceManager.Localize("DUE_TO_CONFLICT_DATA_WILL_BE_OVERWRITTEN");
    }

    protected override void OnCancel(object sender, EventArgs formEventArgs)
    {
      if (base.Active == "ConfirmationPage")
      {
        this.Template = this.EbTemplate.Value;
        this.Destination = this.EbDestination.Value;
        this.Mapping = this.GetMappingResult();
        this.StandartFields = this.ShowStandardField.Checked ? "1" : "0";
        string str = ParametersUtil.NameValueCollectionToXml(this.nvParams);
        if (str.Length == 0)
        {
          str = "-";
        }
        SheerResponse.SetDialogValue(str);
      }
      base.OnCancel(sender, formEventArgs);
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent)
      {
        this.Localize();

        if (!string.IsNullOrEmpty(this.Template))
        {
          this.EbTemplate.Value = this.Template;
        }
        if (!string.IsNullOrEmpty(this.Destination))
        {
          this.EbDestination.Value = this.Destination;
        }
        this.LoadMapping();
        this.EbTemplate.ReadOnly = true;
        this.EbDestination.ReadOnly = true;
        this.ShowStandardField.Checked = this.StandartFields == "1";
      }
    }

    private void OnShowStandardField()
    {
      this.UpdateMapping();
    }

    private void RenderCollisionFieldsWarning()
    {
      Dictionary<string, List<string>> collisionFields = this.GetCollisionFields();
      if (collisionFields.Count == 0)
      {
        this.Collision.Visible = false;
      }
      else
      {
        this.Collision.Visible = true;
        HtmlTextWriter writer = new HtmlTextWriter(new StringWriter());
        writer.Write("<ul>");
        foreach (KeyValuePair<string, List<string>> pair in collisionFields)
        {
          StringBuilder builder = new StringBuilder();
          foreach (string str in pair.Value)
          {
            builder.AppendFormat("{0}</br>", str);
          }
          writer.Write($"<li>{builder}</li>");
        }
        writer.Write("</ul>");
        this.CollisionFields.InnerHtml = writer.InnerWriter.ToString();
      }
    }

    private void RenderLostFieldsWarning()
    {
      List<string> lostFields = this.GetLostFields();
      if (lostFields.Count == 0)
      {
        this.Warning.Visible = false;
      }
      else
      {
        this.Warning.Visible = true;
        HtmlTextWriter writer = new HtmlTextWriter(new StringWriter());
        writer.Write("<ul>");
        foreach (string str in lostFields)
        {
          writer.Write($"<li>{str}</li>");
        }
        writer.Write("</ul>");
        this.LostFields.InnerHtml = writer.InnerWriter.ToString();
      }
    }

    [HandleMessage("dialog:selectdestination", true)]
    protected void SelectDestination(ClientPipelineArgs args)
    {
      if (args.IsPostBack)
      {
        if (args.HasResult)
        {
          Item item = StaticSettings.ContextDatabase.GetItem(args.Result);
          this.EbDestination.Value = item.Paths.FullPath;
        }
      }
      else
      {
        SelectItemOptions options = new SelectItemOptions
        {
          Root = StaticSettings.ContextDatabase.GetItem(ItemIDs.RootID),
          Icon = "Applications/32x32/folder_cubes.png"
        };
        if (this.EbDestination.Value.Length > 0)
        {
          options.SelectedItem = StaticSettings.ContextDatabase.SelectSingleItem(this.EbDestination.Value);
        }
        else if (!string.IsNullOrEmpty(this.Destination))
        {
          options.SelectedItem = StaticSettings.ContextDatabase.SelectSingleItem(this.Destination);
        }
        else
        {
          options.SelectedItem = StaticSettings.ContextDatabase.GetItem(ItemIDs.RootID);
        }
        options.Title = DependenciesManager.ResourceManager.Localize("SELECT_ITEM_TITLE");
        options.Text = DependenciesManager.ResourceManager.Localize("SELECT_ITEM");
        options.ButtonText = DependenciesManager.ResourceManager.Localize("SELECT");
        SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), true);
        args.WaitForPostBack();
      }
    }

    [HandleMessage("dialog:selecttemplate", true)]
    protected void SelectTemplete(ClientPipelineArgs args)
    {
      if (args.IsPostBack)
      {
        if (args.HasResult)
        {
          TemplateItem template = StaticSettings.ContextDatabase.GetTemplate(args.Result);
          this.EbTemplate.Value = template.FullName;
        }
      }
      else
      {
        UrlString str = new UrlString(UIUtil.GetUri("control:Forms.SelectTemplate"));
        str.Append("id", (this.EbTemplate.Value.Length > 0) ? this.EbTemplate.Value : this.Template);
        Context.ClientPage.ClientResponse.ShowModalDialog(str.ToString(), true);
        args.WaitForPostBack();
      }
    }

    public void SetValue(string key, string value)
    {
      if (this.nvParams == null)
      {
        this.nvParams = ParametersUtil.XmlToNameValueCollection(this.Params);
      }
      this.nvParams[key] = value;
    }

    private void UpdateMapping()
    {
      foreach (System.Web.UI.Control control in this.MappingBorder.Controls)
      {
        if (control is TemplateMenu)
        {
          TemplateMenu menu = control as TemplateMenu;
          menu.TemplateID = this.EbTemplate.Value;
          menu.ShowStandardField = this.ShowStandardField.Checked ? "1" : "0";
          typeof(TemplateMenu).GetMethod("Redraw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(menu, new object[] { });
        }
      }
    }

    // Properties
    public Database CurrentDatabase =>
        Factory.GetDatabase(Sitecore.Web.WebUtil.GetQueryString("db"));

    public string CurrentID =>
        Sitecore.Web.WebUtil.GetQueryString("id");

    public string Destination
    {
      get
      {
        return this.GetValueByKey("destination");
      }
      set
      {
        this.SetValue("destination", value);
      }
    }

    public string Mapping
    {
      get
      {
        return this.GetValueByKey("mapping");
      }
      set
      {
        this.SetValue("mapping", value);
      }
    }

    public string Params =>
        (HttpContext.Current.Session[Sitecore.Web.WebUtil.GetQueryString("params")] as string);

    public string StandartFields
    {
      get
      {
        return this.GetValueByKey("showstandartfields");
      }
      set
      {
        this.SetValue("showstandartfields", value);
      }
    }

    public string Template
    {
      get
      {
        return this.GetValueByKey("template");
      }
      set
      {
        this.SetValue("template", value);
      }
    }
  }
}