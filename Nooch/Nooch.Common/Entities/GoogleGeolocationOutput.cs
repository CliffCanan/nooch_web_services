using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class GoogleGeolocationOutput
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string GoogleStatus { get; set; }
        public string CompleteAddress { get; set; }
        public string stateFull { get; set; }
        public string stateAbbrev { get; set; }
        public string Zip { get; set; }
    }
}
