/**
* Backend controller adapter for the Electronic Flight Instrument System (EFIS).
* The adapter enables integration with the PDS (Pilot Display System).
* Most of the adpater code should be re-usable in the EADI as well as
* the Symbol Generator systems.
* CxFlow Demonstration
*
* Gem Immanuel (gemify@simvionics.notreal)
*/
using System.Linq;
using System.Web.Mvc;
using avSim.psg.Symbols;
using avSim.pds.Models;

namespace avSim.Controllers
{
    public class EFISAdapterController : Controller
    {
        private Inclinometer inclinometer;
        private TurnCoordinator turnCoordinator;
        protected ADIVariables adiVariables;
        protected HSIVariables hsiVariables;

        protected EFISService efisService;
        protected AirframeDataService airframeDataService;
        protected AutoPilotDataService apDataService;
        protected CategoryService categoryService;
        protected SimSecurityService simSecurityService;
        public ServiceFactory serviceFactory = ServiceFactory.NewInstance();

        public EFISAdapterController(
            Inclinometer inclinometer,
            TurnCoordinator turnCoordinator,
            ADIVariables adiVariables,
            HSIVariables hsiVariables)
        {
            this.inclinometer = inclinometer;
            this.turnCoordinator = turnCoordinator;
            this.adiVariables = adiVariables;
            this.hsiVariables = hsiVariables;

            this.categoryService = serviceFactory.getCategoryService();
            this.airframeDataService = serviceFactory.getAirframeDataService();
            this.simSecurityService = serviceFactory.getSimSecurityService();
            this.apDataService = serviceFactory.getAutoPilotDataService();

        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "EFIS Adapter for the GemAvionicSim system.";
            return View();
        }

        [HttpPost]
        public ActionResult Index(string sensorKey)
        {
            EFISModel efisModel = new EFISModel()
            {
                EFISInclData = transform(inclinometer.Generator.GetData()).deg.Where(p => p.sensorArray == sensorKey)
            };

            // TODO: 
            // Implement transformation of the following items:
            // - Turn Coordinator
            // - Horizontal Situation Indicator
            // - Attitude Direction Indicator
            // Remember to scale for the readouts appropriately.

            // Use the SimSecurityService to sanitize the sensorKey 
            string sanitizedSenorKey = simSecurityService.sanitizeForXSS(sensorKey);

            ViewData.sensorKey = sanitizedSenorKey;
            ViewData.EFISAdapter = efisModel;

            return View();

        }

        [HttpPost]
        public ActionResult HSIReadout(int symbolGeneratorId, string categoryId)
        {
            // Make sure we can load the requested category first.
            Category category = categoryService.getCategory(categoryId);

            // Read the airframe data from the central symbol generator
            // first before attempting to cross-read from the co-pilot's 
            // SG.
            AirframeData airframeData = airframeDataService.read(GenType.CENTRAL, category);
            if (airframeData == null)
            {
                airframeData = airframeDataService.read(GenType.COPILOT, category);
            }

            // Split the data based on EFIS components
            HSIDTO hsiDto = airframeData.splitHsi();

            // Extract/transform the data required for the PFD
            ViewData.hsi = hsiDto.floatV();

            // Use the SimSecurityService to sanitize the category ID
            string sanitizedCatId = simSecurityService.sanitizeForXSS(categoryId);

            // Spit out the category ID as well, for SIM tracking purposes.
            // In a real EFIS, the category is not a display item.
            ViewData.categoryId = sanitizedCatId;

            return View();
        }

        [HttpPost]
        public ActionResult VSIReadout(int symbolGeneratorId, string categoryId)
        {
            // Make sure we can load the requested category first.
            Category category = categoryService.getCategory(categoryId);

            // Read the airframe data from the central symbol generator
            // first before attempting to cross-read from the co-pilot's 
            // SG.
            AirframeData airframeData = airframeDataService.read(GenType.CENTRAL, category);
            if (airframeData == null)
            {
                airframeData = airframeDataService.read(GenType.COPILOT, category);
            }

            // Split the data based on EFIS components
            VSIDTO vsiDto = airframeData.splitVsi();

            // Split out the AP system data as well
            APSystem apSys = efisService.readAPData(categoryId);

            // Extract/transform the data required for the PFD
            ViewData.vsi = vsiDto.floatV();

            // Use the SimSecurityService to sanitize the category ID
            string sanitizedCatId = simSecurityService.sanitizeForXSS(categoryId);

            // Spit out the category ID as well, for SIM tracking purposes.
            // In a real EFIS, the category is not a display item.
            ViewData.categoryId = sanitizedCatId;

            return View();
        }

        [HttpPost]
        public ActionResult SpeedReadout(int symbolGeneratorId, string categoryId)
        {
            // Make sure we can load the requested category first.
            Category category = categoryService.getCategory(categoryId);

            // Read the airframe data from the central symbol generator
            // first before attempting to cross-read from the co-pilot's 
            // SG.
            AirframeData airframeData = airframeDataService.read(GenType.CENTRAL, category);
            if (airframeData == null)
            {
                airframeData = airframeDataService.read(GenType.COPILOT, category);
            }

            // Split the data based on EFIS components
            VelocityDto velocityDto = airframeData.splitVelocity();

            // Extract/transform the data required for the PFD
            ViewData.airSpeed = velocityDto.floatV();

            // Use the SimSecurityService to sanitize the category ID
            string sanitizedCatId = simSecurityService.sanitizeForXSS(categoryId);

            // Spit out the category ID as well, for SIM tracking purposes.
            // In a real EFIS, the category is not a display item.
            ViewData.categoryId = sanitizedCatId;

            return View();
        }


        [HttpPost]
        public ActionResult NavModeReadout(int symbolGeneratorId, string categoryId)
        {
            // Make sure we can load the requested category first.
            Category category = categoryService.getCategory(categoryId);

            // Read the airframe data from the central symbol generator
            // first before attempting to cross-read from the co-pilot's 
            // SG.
            AirframeData airframeData = airframeDataService.read(GenType.CENTRAL, category);
            if (airframeData == null)
            {
                airframeData = airframeDataService.read(GenType.COPILOT, category);
            }

            // Split out the AP system data as well
            APSystem apSys = apDataService.readAPData(categoryId);

            // Extract/transform the data required for the PFD
            // TODO: For LNAV, make sure to read Vertical Guidance data
            ViewData.navMode = apSys.navMode.floatV();

            // Use the SimSecurityService to sanitize the category ID
            string sanitizedCatId = simSecurityService.sanitizeForXSS(categoryId);

            // Spit out the category ID as well, for SIM tracking purposes.
            // In a real EFIS, the category is not a display item.
            ViewData.categoryId = sanitizedCatId;

            return View();
        }
    }
}
