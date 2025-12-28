using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;

namespace MonitoringServiceCore.Pages
{
    public class IndexModel : PageModel
    {
        private MonitoringDbContext _dbContext;
        public List<User> users = new List<User>();
        public IndexModel(MonitoringDbContext dbContext)
        {
           _dbContext = dbContext;
        }

        public  void OnGet()
        {
            users = _dbContext.Users
                .ToList();
        }
    }
}
