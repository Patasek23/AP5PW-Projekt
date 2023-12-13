using System;
using System.Data;
using System.Drawing;
using System.Web.Mvc;
using Data;
using DayPilot.Web.Mvc;
using DayPilot.Web.Mvc.Data;
using DayPilot.Web.Mvc.Enums;
using DayPilot.Web.Mvc.Events.Scheduler;

namespace TutorialCS.Controllers
{

    public class SchedulerController : Controller
    {

        public ActionResult Backend()
        {
            return new Scheduler().CallBack(this);
        }

        class Scheduler : DayPilotScheduler
        {
            protected override void OnInit(InitArgs e)
            {
                DateTime start = new DateTime(2018, 1, 1, 12, 0, 0);
                DateTime end = new DateTime(2019, 1, 1, 12, 0, 0);

                Timeline = new TimeCellCollection();
                for (DateTime cell = start; cell < end; cell = cell.AddDays(1))
                {
                    Timeline.Add(cell, cell.AddDays(1));
                }

                LoadRoomsAndReservations();
                ScrollTo(DateTime.Today.AddDays(-1));
                Separators.Add(DateTime.Now, Color.Red);
                UpdateWithMessage("Vítejte u nás!", CallBackUpdateType.Full);
            }
            private void LoadRoomsAndReservations()
            {
                LoadRooms();
                LoadReservations();
            }
            private void LoadReservations()
            {
                Events = Db.GetReservations().Rows;

                DataStartField = "ReservationStart";
                DataEndField = "ReservationEnd";
                DataIdField = "ReservationId";
                DataTextField = "ReservationName";
                DataResourceField = "RoomId";

                DataTagFields = "ReservationStatus";

            }
            private void LoadRooms()
            {
                Resources.Clear();

                string roomFilter = "0";
                if (ClientState["filter"] != null)
                {
                    roomFilter = (string)ClientState["filter"]["room"];
                }

                DataTable dt = Db.GetRoomsFiltered(roomFilter);

                foreach (DataRow r in dt.Rows)
                {
                    string name = (string)r["RoomName"];
                    string id = Convert.ToString(r["RoomId"]);
                    string status = (string)r["RoomStatus"];
                    int beds = Convert.ToInt32(r["RoomSize"]);
                    string bedsFormatted = (beds == 1) ? "1 postel" : String.Format("{0} postele", beds);

                    Resource res = new Resource(name, id);
                    res.DataItem = r;
                    res.Columns.Add(new ResourceColumn(bedsFormatted));
                    res.Columns.Add(new ResourceColumn(status));

                    Resources.Add(res);
                }
            }
            protected override void OnEventMove(EventMoveArgs e)
            {
                string id = e.Id;
                DateTime start = e.NewStart;
                DateTime end = e.NewEnd;
                string resource = e.NewResource;

                string message = null;
                if (!Db.IsFree(id, start, end, resource))
                {
                    message = "The reservation cannot overlap with an existing reservation.";
                }
                else if (e.OldEnd <= DateTime.Today)
                {
                    message = "This reservation cannot be changed anymore.";
                }
                else if (e.NewStart < DateTime.Today)
                {
                    message = "The reservation cannot be moved to the past.";
                }
                else
                {
                    Db.MoveReservation(e.Id, e.NewStart, e.NewEnd, e.NewResource);
                }
                
                LoadReservations();
                UpdateWithMessage(message);
            }
            protected override void OnEventResize(EventResizeArgs e)
            {
                Db.MoveReservation(e.Id, e.NewStart, e.NewEnd, e.Resource);
                LoadReservations();
                Update();
            }

            protected override void OnBeforeEventRender(BeforeEventRenderArgs e)
            {
                e.Html = String.Format("{0} ({1:d} - {2:d})", e.Text, e.Start, e.End);
                int status = Convert.ToInt32(e.Tag["ReservationStatus"]);

                switch (status)
                {
                    case 0:
                        if (e.Start < DateTime.Today.AddDays(2))
                        {
                            e.DurationBarColor = "red";
                            e.ToolTip = "Platnost vypršela (nepotvrzeno včas)";
                        }
                        else
                        {
                            e.DurationBarColor = "orange";
                            e.ToolTip = "Nový";
                        }
                        break;
                    case 1:  
                        if (e.Start < DateTime.Today || (e.Start == DateTime.Today && DateTime.Now.TimeOfDay.Hours > 18))  
                        {
                            e.DurationBarColor = "#f41616";  
                            e.ToolTip = "Late arrival";
                        }
                        else
                        {
                            e.DurationBarColor = "green";
                            e.ToolTip = "Potvrzeno";
                        }
                        break;
                    case 2: 
                        if (e.End < DateTime.Today || (e.End == DateTime.Today && DateTime.Now.TimeOfDay.Hours > 11))  
                        {
                            e.DurationBarColor = "#f41616"; 
                            e.ToolTip = "Pozdní odjezd";
                        }
                        else
                        {
                            e.DurationBarColor = "#1691f4";  
                            e.ToolTip = "Aktivní";
                        }
                        break;
                    case 3: // checked out
                        e.DurationBarColor = "gray";
                        e.ToolTip = "Ukončeno";
                        break;
                    default:
                        throw new ArgumentException("Unexpected status.");
                }

                e.Html = String.Format("<div>{0}<br /><span style='color:gray'>{1}</span></div>", e.Html, e.ToolTip);

                int paid = Convert.ToInt32(e.DataItem["ReservationPaid"]);
                string paidColor = "#aaaaaa";

                e.Areas.Add(new Area().Bottom(6).Right(4).Html("<div style='color:" + paidColor + "; font-size: 8pt;'>Paid: " + paid + "%</div>").Visible());
                e.Areas.Add(new Area().Left(4).Bottom(4).Right(4).Height(2).Html("<div style='background-color:" + paidColor + "; height: 100%; width:" + paid + "%'></div>").Visible());
            }
            protected override void OnBeforeResHeaderRender(BeforeResHeaderRenderArgs e)
            {
                string status = (string)e.DataItem["RoomStatus"];
                switch (status)
                {
                    case "Dirty":
                        e.CssClass = "status_dirty";
                        break;
                    case "Cleanup":
                        e.CssClass = "status_cleanup";
                        break;
                }
            }
            protected override void OnCommand(CommandArgs e)
            {
                switch (e.Command)
                {
                    case "refresh":
                        LoadReservations();
                        UpdateWithMessage("Refreshed");
                        break;
                    case "filter":
                        LoadRoomsAndReservations();
                        UpdateWithMessage("Updated", CallBackUpdateType.Full);
                        break;
                }
            }
        }

    }

}
