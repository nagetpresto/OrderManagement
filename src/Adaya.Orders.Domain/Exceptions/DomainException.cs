using System;
using System.Collections.Generic;
using System.Text;

namespace Adaya.Orders.Domain.Exceptions;

public class DomainException(string message) : Exception(message); 
