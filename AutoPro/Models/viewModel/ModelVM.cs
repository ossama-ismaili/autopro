using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoPro.Models.viewModel
{
    public class ModelVM
    {
        public Model Model { get; set; }
        public IEnumerable<Marque> Marques { get; set; }
    }
}
