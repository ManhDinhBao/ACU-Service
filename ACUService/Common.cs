using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ACUService
{
    class Common
    {
        public enum FunctionCode
        {
            GET_ACU_VERSION = 0x0000,
            RESTART_ACU = 0x0001,
            SET_STATIC_IP = 0x0002,
            T_DEFAULT_IP = 0x0003,
            SET_NTP_SERVER = 0x0004,
            SET_SERIAL_NUMBER = 0x0005,
            GET_SERIAL_NUMBER = 0x0006,
            UPDATE_NTP = 0x0007,
            SET_RTC = 0x0008,
            GET_RTC = 0x0009,
            SET_PERMISSION = 0x000A,
            GET_PERMISSION,
            ADD_CARD = 0x000C,
            DELETE_CARD = 0x000D,
            GET_CARD,
            GET_CARD_QUANTITY = 0x000F,
            CLEAR_CARD = 0x0011,
            SEND_EVENT_ACK = 0x0010

        }
        public enum FunctionCode_Respone
        {
            GET_ACU_VERSION_RSP = 0x0100,
            RESTART_ACU_RSP = 0x0101,
            SET_STATIC_IP_RSP = 0x0102,
            T_DEFAULT_IP_RSP = 0x0103,
            SET_NTP_SERVER_RSP = 0x0104,
            SET_SERIAL_NUMBER_RSP = 0x0105,
            GET_SERIAL_NUMBER_RSP = 0x0106,
            UPDATE_NTP_RSP = 0x0107,
            SET_RTC_RSP = 0x0108,
            GET_RTC_RSP = 0x0109,
            SET_PERMISSION_RSP = 0x010A,
            GET_PERMISSION_RSP,
            ADD_CARD_RSP = 0x010C,
            DELETE_CARD_RSP = 0x010D,
            GET_CARD_RSP,
            GET_CARD_QUANTITY_RSP = 0x010F,
            SEND_EVENT_RSP = 0x0110,
            CLEAR_CARD_RSP = 0x0111,
            KEEP_ALIVE_RSP = 0x0112

        }

        public static string GetEnumName(string value)
        {
            string enumName = null;
            Int16[] enumValues = new Int16[] { 0x0100, 0x0101, 0x0003, 0x0104, 0x0105, 0x0106, 0x0107, 0x0108, 0x0109, 0x010A, 0x010B, 0x010C, 0x010D, 0x010F, 0x0110, 0x0111, 0x0112 };
            foreach (int enumValue in enumValues)
            {
                string k = string.Format("0x{0}", enumValue.ToString("X4"));
                if (k == value)
                {
                    enumName = Enum.GetName(typeof(FunctionCode_Respone), enumValue);
                }

            }
            return enumName;
        }
    }
}
