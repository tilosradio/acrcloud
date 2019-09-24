using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace TilosAzureMvc.Controllers
{
    public class EchoController : Controller
    {
        private static string _lastPost;

        // GET: Echo
        [AllowCrossSite]
        public ActionResult Index()
        {
            return Content(_lastPost);
            return View();
        }

        // GET: Echo/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Echo/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Echo/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                StringBuilder msg = new StringBuilder();
                foreach (var key in collection.AllKeys) {
                    msg.Append(key.ToString());
                    msg.Append("=");
                    msg.Append(collection[key].ToString());
                    msg.Append(";");
                }
                _lastPost = msg.ToString();
                return Content(msg.ToString());
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Echo/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Echo/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Echo/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Echo/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
