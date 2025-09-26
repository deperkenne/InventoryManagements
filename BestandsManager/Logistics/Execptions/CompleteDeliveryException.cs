using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BestandsManager.Logistics.Execptions
{
     public class CompleteDeliveryException: Exception
    {   
        public CompleteDeliveryException(string message) : base(message)
        {
        }
    }
}
