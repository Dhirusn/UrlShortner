using UrlShortener.Application.Interfaces;
using UrlShortener.Data.Models;
using UrlShortener.Models.ViewModels;

namespace UrlShortener.Services
{
    public class PricingService : IPricingService
    {
        public async Task<PricingViewModel> GetPricingPageDataAsync()
        {
            // In a real app, this would come from a database
            // For now, we'll return hardcoded data
            await Task.Delay(1); // Simulate async operation

            var viewModel = new PricingViewModel
            {
                PageTitle = "Find a Plan That Meets Your Needs",
                PageDescription = "Choose the perfect plan for your business with our flexible pricing options.",
                BrandedLinksDescription = "The Group's products and services are provided by the Group to ensure that they are able to meet their needs. We have a number of brands that are not available for our customers.",
                FeatureBreakdownDescription = "This is a special issue, an exciting topic in Scotland to work alongside people on your own brand and help us make things better.",
                IsAnnualBilling = true,
                Plans = GetSamplePlans(),
                FeatureCategories = GetFeatureCategories(),
                FAQs = GetFAQs()
            };

            return viewModel;
        }

        private List<PricingPlan> GetSamplePlans()
        {
            return new List<PricingPlan>
            {
                new PricingPlan
                {
                    Id = 1,
                    Name = "Basic",
                    Description = "Perfect for individuals and small projects",
                    MonthlyPrice = 19.99m,
                    YearlyPrice = 191.90m, // 20% discount
                    IsPopular = false,
                    RecommendedFor = "Startups & Individuals",
                    ButtonText = "Start Free Trial",
                    Features = new List<PricingFeature>
                    {
                        new PricingFeature { Name = "Up to 1,000 branded links", IsAvailable = true },
                        new PricingFeature { Name = "Basic analytics", IsAvailable = true },
                        new PricingFeature { Name = "1 custom domain", IsAvailable = true },
                        new PricingFeature { Name = "Standard support", IsAvailable = true },
                        new PricingFeature { Name = "5 team members", IsAvailable = true },
                        new PricingFeature { Name = "Advanced reporting", IsAvailable = false },
                        new PricingFeature { Name = "API access", IsAvailable = false },
                        new PricingFeature { Name = "Priority support", IsAvailable = false }
                    }
                },
                new PricingPlan
                {
                    Id = 2,
                    Name = "Professional",
                    Description = "Ideal for growing businesses",
                    MonthlyPrice = 49.99m,
                    YearlyPrice = 479.90m, // 20% discount
                    IsPopular = true,
                    RecommendedFor = "Growing Businesses",
                    ButtonText = "Start Free Trial",
                    ButtonCssClass = "btn-success",
                    Features = new List<PricingFeature>
                    {
                        new PricingFeature { Name = "Up to 10,000 branded links", IsAvailable = true },
                        new PricingFeature { Name = "Advanced analytics", IsAvailable = true },
                        new PricingFeature { Name = "5 custom domains", IsAvailable = true },
                        new PricingFeature { Name = "Priority support", IsAvailable = true },
                        new PricingFeature { Name = "20 team members", IsAvailable = true },
                        new PricingFeature { Name = "Advanced reporting", IsAvailable = true },
                        new PricingFeature { Name = "Basic API access", IsAvailable = true },
                        new PricingFeature { Name = "Dedicated account manager", IsAvailable = false }
                    }
                },
                new PricingPlan
                {
                    Id = 3,
                    Name = "Enterprise",
                    Description = "For large organizations with complex needs",
                    MonthlyPrice = 99.99m,
                    YearlyPrice = 959.90m, // 20% discount
                    IsPopular = false,
                    RecommendedFor = "Large Organizations",
                    ButtonText = "Contact Sales",
                    ButtonCssClass = "btn-warning",
                    Features = new List<PricingFeature>
                    {
                        new PricingFeature { Name = "Unlimited branded links", IsAvailable = true },
                        new PricingFeature { Name = "Advanced analytics & reporting", IsAvailable = true },
                        new PricingFeature { Name = "Unlimited custom domains", IsAvailable = true },
                        new PricingFeature { Name = "24/7 priority support", IsAvailable = true },
                        new PricingFeature { Name = "100+ team members", IsAvailable = true },
                        new PricingFeature { Name = "Full API access", IsAvailable = true },
                        new PricingFeature { Name = "Dedicated account manager", IsAvailable = true },
                        new PricingFeature { Name = "Custom integrations", IsAvailable = true }
                    }
                }
            };
        }

        private List<FeatureCategory> GetFeatureCategories()
        {
            return new List<FeatureCategory>
            {
                new FeatureCategory
                {
                    Name = "Healthy Universe",
                    Description = "Environmental and sustainability features",
                    IconClass = "fas fa-globe",
                    Features = new List<FeatureItem>
                    {
                        new FeatureItem { Name = "Lake Management Tools", IconClass = "fas fa-water", ColorClass = "text-primary" },
                        new FeatureItem { Name = "Vegetarian & Natural Options", IconClass = "fas fa-leaf", ColorClass = "text-success" },
                        new FeatureItem { Name = "Environmental Analytics", IconClass = "fas fa-chart-line", ColorClass = "text-info" },
                        new FeatureItem { Name = "Sustainability Tracking", IconClass = "fas fa-recycle", ColorClass = "text-warning" }
                    }
                },
                new FeatureCategory
                {
                    Name = "Life & Weighting & Consumption",
                    Description = "Resource management and analytics",
                    IconClass = "fas fa-balance-scale",
                    Features = new List<FeatureItem>
                    {
                        new FeatureItem { Name = "Resource Allocation", IconClass = "fas fa-balance-scale", ColorClass = "text-primary" },
                        new FeatureItem { Name = "Impact Measurement", IconClass = "fas fa-weight", ColorClass = "text-success" },
                        new FeatureItem { Name = "Consumption Analytics", IconClass = "fas fa-chart-pie", ColorClass = "text-info" },
                        new FeatureItem { Name = "Performance Metrics", IconClass = "fas fa-tachometer-alt", ColorClass = "text-warning" }
                    }
                }
            };
        }

        private List<FAQ> GetFAQs()
        {
            return new List<FAQ>
            {
                new FAQ
                {
                    Question = "Can I switch plans at any time?",
                    Answer = "Yes, you can upgrade or downgrade your plan at any time. Changes will be prorated and applied to your next billing cycle."
                },
                new FAQ
                {
                    Question = "Is there a free trial available?",
                    Answer = "Yes, all plans come with a 14-day free trial. No credit card is required to start your trial."
                },
                new FAQ
                {
                    Question = "What payment methods do you accept?",
                    Answer = "We accept all major credit cards, PayPal, and bank transfers for annual plans."
                },
                new FAQ
                {
                    Question = "Can I cancel my subscription?",
                    Answer = "Yes, you can cancel your subscription at any time. There are no cancellation fees."
                }
            };
        }

        public async Task<PricingPlan> GetPlanByIdAsync(int id)
        {
            var plans = GetSamplePlans();
            return await Task.FromResult(plans.FirstOrDefault(p => p.Id == id));
        }

        public async Task<List<FAQ>> GetFAQsAsync()
        {
            return await Task.FromResult(GetFAQs());
        }

        public async Task<string> StartTrialAsync(PlanSelectionModel model)
        {
            // In a real application, this would:
            // 1. Validate the model
            // 2. Create a user account
            // 3. Set up the subscription
            // 4. Send welcome email
            // 5. Return a success message or redirect URL

            await Task.Delay(100); // Simulate async operation

            // For demo purposes, return a success message
            return $"Trial started successfully for plan {model.PlanId} with {model.BillingPeriod} billing. Confirmation sent to {model.Email}";
        }
    }
}
