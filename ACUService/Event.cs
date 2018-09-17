using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ACUService
{
    class Event
    {
        public Event() { }

        public Event(string eventId, DateTime eventDate, string device, string functionCode, string cardNo, string doorId, string status)
        {
            EventId = eventId;
            EventDate = eventDate;
            Device = device;
            FunctionCode = functionCode;
            CardNo = cardNo;
            DoorId = doorId;
            Status = status;
        }

        private string eventId;

        public string EventId
        {
            get { return eventId; }
            set { eventId = value; }
        }

        private DateTime eventDate;

        public DateTime EventDate
        {
            get { return eventDate; }
            set { eventDate = value; }
        }

        private string device;

        public string Device
        {
            get { return device; }
            set { device = value; }
        }

        private string functionCode;

        public string FunctionCode
        {
            get { return functionCode; }
            set { functionCode = value; }
        }

        private string cardNo;

        public string CardNo
        {
            get { return cardNo; }
            set { cardNo = value; }
        }

        private string doorId;

        public string DoorId
        {
            get { return doorId; }
            set { doorId = value; }
        }

        private string status;

        public string Status
        {
            get { return status; }
            set { status = value; }
        }

        public string AddEvent()
        {
            string result = null;
            DataTable dt = null;
            try
            {
                ServiceReference1.WSACUSoapClient client = new ServiceReference1.WSACUSoapClient();
                DataSet ds = client.EventSave("A", eventId, eventDate, device, functionCode, cardNo, doorId, status);
                dt = ds.Tables[0];
                result = dt.Rows[0][0].ToString();
                return result;
            }

            catch (Exception ex)
            {
                return result;
            }
        }
    }
}
