﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orc.ReactProcessor.Core.TypeHelpers
{
    /// <summary>
    /// Represents an JavaScript <code>undefined</code> type
    /// </summary>
    public sealed class Undefined
    {
        /// <summary>
        /// Gets a one and only <code>undefined</code> instance
        /// </summary>
        public static readonly Undefined Value = new Undefined();

        private Undefined()
        { }

        /// <summary>
        /// Returns a string that represents the current object
        /// </summary>
        /// <returns>A string that represents the current object</returns>
        public override string ToString()
        {
            return "undefined";
        }
    }
}
