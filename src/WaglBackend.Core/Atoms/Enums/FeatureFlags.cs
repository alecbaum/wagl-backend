using System.ComponentModel;

namespace WaglBackend.Core.Atoms.Enums;

[Flags]
public enum FeatureFlags : long
{
    [Description("No features")]
    None = 0,
    
    [Description("Basic API access")]
    BasicApi = 1 << 0,
    
    [Description("Advanced API features")]
    AdvancedApi = 1 << 1,
    
    [Description("Premium API features")]
    PremiumApi = 1 << 2,
    
    [Description("Analytics dashboard")]
    Analytics = 1 << 3,
    
    [Description("Custom integrations")]
    CustomIntegrations = 1 << 4,
    
    [Description("Webhook support")]
    Webhooks = 1 << 5,
    
    [Description("Real-time notifications")]
    RealTimeNotifications = 1 << 6,
    
    [Description("Bulk operations")]
    BulkOperations = 1 << 7,
    
    [Description("Priority support")]
    PrioritySupport = 1 << 8,
    
    [Description("24x7 support")]
    TwentyFourSevenSupport = 1 << 9,
    
    [Description("Export functionality")]
    DataExport = 1 << 10,
    
    [Description("White-label options")]
    WhiteLabel = 1 << 11,
    
    [Description("All Tier1 features")]
    Tier1Features = BasicApi | PrioritySupport,
    
    [Description("All Tier2 features")]
    Tier2Features = Tier1Features | AdvancedApi | Analytics | Webhooks | DataExport,
    
    [Description("All Tier3 features")]
    Tier3Features = Tier2Features | PremiumApi | CustomIntegrations | RealTimeNotifications | 
                   BulkOperations | TwentyFourSevenSupport | WhiteLabel
}