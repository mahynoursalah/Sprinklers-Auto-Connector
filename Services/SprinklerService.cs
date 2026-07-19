using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sprinklers_Connectors.Extensions;

namespace Sprinklers_Connectors.Services
{
    public class SprinklerService
    {
        public List <FamilyInstance> GetSprinklers(Document document)
        {

           return new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_Sprinklers)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();
        }

        public List<FamilyInstance> GetUnconnectedSprinklers(Document document)
        {
            return GetSprinklers(document).Where(sprinkler=>sprinkler.GetConnectors().Any(Connector=> !Connector.IsConnected))
                .ToList();
        }
    }
}
