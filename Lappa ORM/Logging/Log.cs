// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Lappa_ORM.Logging
{
    internal class Log
    {
        static MethodInfo messageBoxShowMethod;
        static object okButton;

        public static void Initialize()
        {
            if (Console.IsErrorRedirected && messageBoxShowMethod == null)
            {
                var assembly = Assembly.Load("System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                var type = assembly.GetType("System.Windows.Forms.MessageBox");

                messageBoxShowMethod = type.GetMethod("Show", new[] { typeof(string), typeof(string), assembly.GetType("System.Windows.Forms.MessageBoxButtons") });

                // Get MessageBoxButtons.Ok value.
                okButton = assembly.GetType("System.Windows.Forms.MessageBoxButtons").GetEnumValues().GetValue(0);
            }
        }

        public static void Error(string message)
        {
            if (Console.IsErrorRedirected)
                messageBoxShowMethod.Invoke(null, new object[] { message, "Error", okButton });
            else
            {
                Console.WriteLine(message);
                Console.ReadKey();
            }
        }
    }
}
