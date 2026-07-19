using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sprinklers_Connectors.Extensions
{
    public static class FamilyInstanceExtensions
    {
        public static List<Connector> GetConnectors(this FamilyInstance familyInstance)
        {
          var ConnectorManager = familyInstance.MEPModel?.ConnectorManager;

            if (ConnectorManager == null)
            {
                return new List<Connector>();
            }

            return familyInstance.MEPModel.ConnectorManager.Connectors.Cast<Connector>().ToList();
        }
    }
}
