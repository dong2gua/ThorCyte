﻿using System.Globalization;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace ThorCyte.ProtocolModule.Utils
{
    public class RequiredValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult(false, "Content can not be empty!");
            }
            return new ValidationResult(true, null);
        }
    }

    public class RequirIntegerValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {

            //determine if this is a Integer
            return IsInteger(value.ToString())
                ? new ValidationResult(true, null)
                : new ValidationResult(false, "Please input an integer!");
        }

        public bool IsInteger(string value)
        {
            var r = new Regex(@"^-?[0-9]\d*$");
            return r.IsMatch(value);
        }
    }


    public class RequireNotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if(value == null) return new ValidationResult(false,null);
            return value.ToString().Trim().Length == 0
                ? new ValidationResult(false, "Input can not be empty!")
                : new ValidationResult(true, null);
        }
    }


}