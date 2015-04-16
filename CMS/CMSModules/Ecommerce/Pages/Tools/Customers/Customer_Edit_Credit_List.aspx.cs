using System;

using CMS.Core;
using CMS.Ecommerce;
using CMS.ExtendedControls.ActionsConfig;
using CMS.Helpers;
using CMS.Base;
using CMS.SiteProvider;
using CMS.UIControls;

[UIElement(ModuleName.ECOMMERCE, "Customers.Credit")]
public partial class CMSModules_Ecommerce_Pages_Tools_Customers_Customer_Edit_Credit_List : CMSCustomersPage
{
    #region "Variables"

    private int customerId;
    private CustomerInfo customerObj;
    private int creditCurrencySiteId = -1;
    private CurrencyInfo currency;

    #endregion


    #region "Page Events"

    protected void Page_Load(object sender, EventArgs e)
    {
        // Get site id of credits main currency
        creditCurrencySiteId = ECommerceHelper.GetSiteID(SiteContext.CurrentSiteID, ECommerceSettings.USE_GLOBAL_CREDIT);

        // Get currency in which credit is expressed in
        currency = CurrencyInfoProvider.GetMainCurrency(creditCurrencySiteId);

        // Get customerId from url
        customerId = QueryHelper.GetInteger("customerid", 0);

        // Load customer info
        customerObj = CustomerInfoProvider.GetCustomerInfo(customerId);

        // Check if customer belongs to current site
        if (!CheckCustomerSiteID(customerObj))
        {
            customerObj = null;
        }

        // Check, if edited customer exists
        EditedObject = customerObj;

        // Init unigrid
        UniGrid.HideControlForZeroRows = true;
        UniGrid.OnAction += uniGrid_OnAction;
        UniGrid.OnExternalDataBound += UniGrid_OnExternalDataBound;
        UniGrid.OrderBy = "EventDate DESC, EventName ASC";
        UniGrid.WhereCondition = "EventCustomerID = " + customerId + " AND ISNULL(EventSiteID, 0) = " + creditCurrencySiteId;

        if (customerObj != null)
        {
            InitializeMasterPage();

            // Configuring global credit
            if (creditCurrencySiteId == 0)
            {
                var site = SiteContext.CurrentSite;
                if (site != null)
                {
                    // Show "using global credit" info message
                    ShowInformation(string.Format(GetString("com.UsingGlobalSettings"), site.DisplayName, GetString("com.ui.creditevents")));
                }
            }

            // Display customer total credit        
            headTotalCredit.Text = string.Format(GetString("CreditEvent_List.TotalCredit"), GetFormattedTotalCredit());
        }
    }

    #endregion


    #region "Private Methods"

    /// <summary>
    /// Initializes the master page elements.
    /// </summary>
    private void InitializeMasterPage()
    {
        // Set the action link
        CurrentMaster.HeaderActions.ActionsList.Add(new HeaderAction
        {
            Text = GetString("CreditEvent_List.NewItemCaption"),
            RedirectUrl = "~/CMSModules/Ecommerce/Pages/Tools/Customers/Customer_Edit_Credit_Edit.aspx?customerid=" + customerId + "&siteId=" + SiteID
        });
    }


    /// <summary>
    /// Returns formatted total credit string.
    /// </summary>
    private string GetFormattedTotalCredit()
    {
        // Get total credit
        double totalCredit = CreditEventInfoProvider.GetTotalCredit(customerId, SiteContext.CurrentSiteID);

        // Return formatted total credit according to the credits main currency format string
        return CurrencyInfoProvider.GetFormattedPrice(totalCredit, currency);
    }

    #endregion


    #region "Event Handlers"

    protected object UniGrid_OnExternalDataBound(object sender, string sourceName, object parameter)
    {
        // Show only date part from date-time value
        switch (sourceName.ToLowerCSafe())
        {
            case "eventcreditchange":
                return CurrencyInfoProvider.GetFormattedPrice(ValidationHelper.GetDouble(parameter, 0), currency);
        }

        return parameter;
    }


    /// <summary>
    /// Handles the UniGrid's OnAction event.
    /// </summary>
    /// <param name="actionName">Name of item (button) that throws event</param>
    /// <param name="actionArgument">ID (value of Primary key) of corresponding data row</param>
    protected void uniGrid_OnAction(string actionName, object actionArgument)
    {
        if (actionName == "edit")
        {
            URLHelper.Redirect("Customer_Edit_Credit_Edit.aspx?customerid=" + customerId + "&eventid=" + Convert.ToString(actionArgument) + "&siteId=" + SiteID);
        }
        else if (actionName == "delete")
        {
            // Check customer modification permission
            if (!ECommerceContext.IsUserAuthorizedToModifyCustomer())
            {
                RedirectToAccessDenied("CMS.Ecommerce", "EcommerceModify OR ModifyCustomers");
            }

            // Check if using global credit
            if (ECommerceSettings.UseGlobalCredit(SiteContext.CurrentSiteName))
            {
                // Check Ecommerce global modify permission
                if (!ECommerceContext.IsUserAuthorizedForPermission("EcommerceGlobalModify"))
                {
                    RedirectToAccessDenied("CMS.Ecommerce", "EcommerceGlobalModify");
                }
            }

            // Get event info object
            int eventId = Convert.ToInt32(actionArgument);
            CreditEventInfo eventInfo = CreditEventInfoProvider.GetCreditEventInfo(eventId);

            // Check if deleted event exists and whether it belongs to edited customer
            if ((eventInfo != null) && (eventInfo.EventCustomerID == customerId))
            {
                // Delete CreditEventInfo object from database
                CreditEventInfoProvider.DeleteCreditEventInfo(eventInfo);
                UniGrid.ReloadData();
                // Display customer total credit        
                headTotalCredit.Text = string.Format(GetString("CreditEvent_List.TotalCredit"), GetFormattedTotalCredit());
            }
        }
    }

    #endregion
}