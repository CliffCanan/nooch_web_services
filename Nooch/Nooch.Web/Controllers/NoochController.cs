using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Nooch.Web.Controllers
{
    public class NoochController : Controller
    {
        // GET: Nooch
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult AddBank()
        {
            return View();
        }
        public ActionResult BankVerification()
        {
            return View();
        }
    }
}