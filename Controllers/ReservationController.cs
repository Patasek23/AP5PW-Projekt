using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Mvc;
using Data;
using DayPilot.Web.Mvc.Json;
using DayPilot.Web.Mvc.Recurrence;
using TutorialCS;
public class ReservationController : Controller
{
    public ActionResult Edit(string id)
    {
        DataRow dr = Db.GetReservation(id);

        if (dr == null)
        {
            throw new Exception("The task was not found");
        }

        return View(new
        {
            Id = id,
            Text = dr["ReservationName"],
            Start = Convert.ToDateTime(dr["ReservationStart"]).ToShortDateString(),
            End = Convert.ToDateTime(dr["ReservationEnd"]).ToShortDateString(),
            Status = new SelectList(new SelectListItem[]
            {
                new SelectListItem { Text = "Nový", Value = "0"},
                new SelectListItem { Text = "Potvrzeno", Value = "1"},
                new SelectListItem { Text = "Aktivní", Value = "2"},
                new SelectListItem { Text = "Ukončeno", Value = "3"}
            }, "Value", "Text", dr["ReservationStatus"]),
            Paid = new SelectList(new SelectListItem[]
            {
                new SelectListItem { Text = "0%", Value = "0"},
                new SelectListItem { Text = "50%", Value = "50"},
                new SelectListItem { Text = "100%", Value = "100"},
            }, "Value", "Text", dr["ReservationPaid"]),
            Resource = new SelectList(Db.GetRoomSelectList(), "Value", "Text", dr["RoomId"])
        });
    }

    [AcceptVerbs(HttpVerbs.Post)]
    public ActionResult Edit(FormCollection form)
    {
        string id = form["Id"];
        string name = form["Text"];
        DateTime start = Convert.ToDateTime(form["Start"]).Date.AddHours(12);
        DateTime end = Convert.ToDateTime(form["End"]).Date.AddHours(12);
        string resource = form["Resource"];
        int paid = Convert.ToInt32(form["Paid"]);
        int status = Convert.ToInt32(form["Status"]);

        DataRow dr = Db.GetReservation(id);

        if (dr == null)
        {
            throw new Exception("The task was not found");
        }

        Db.UpdateReservation(id, name, start, end, resource, status, paid);

        return JavaScript(SimpleJsonSerializer.Serialize("OK"));
    }
    public ActionResult Create()
    {
        return View(new 
        {
            Start = Convert.ToDateTime(Request.QueryString["start"]).ToShortDateString(),
            End = Convert.ToDateTime(Request.QueryString["end"]).ToShortDateString(),
            Resource = new SelectList(Db.GetRoomSelectList(), "Value", "Text", Request.QueryString["resource"])
        });
    }

    [AcceptVerbs(HttpVerbs.Post)]
    public ActionResult Create(FormCollection form)
    {
        DateTime start = Convert.ToDateTime(form["Start"]).Date.AddHours(12);
        DateTime end = Convert.ToDateTime(form["End"]).Date.AddHours(12);
        string text = form["Text"];
        string resource = form["Resource"];

        Db.CreateReservation(start, end, resource, text);
        return JavaScript(SimpleJsonSerializer.Serialize("OK"));
    }
}
