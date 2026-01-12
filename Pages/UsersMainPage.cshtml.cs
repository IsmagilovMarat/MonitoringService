using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.SiteAnalysisNamespace;

namespace MonitoringServiceCore.Pages
{
    public class UsersMainPageModel : PageModel
    {
        private MonitoringDbContext _dbContext;
        public UsersMainPageModel(MonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public List<SiteAnalysis> SitesList = new  List<SiteAnalysis>();
        public void OnGet()
        {
            
            SitesList = _dbContext.SiteAnalyses.ToList();
        }


    }
}
