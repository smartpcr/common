// -----------------------------------------------------------------------
// <copyright file="Person.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Tests.Models
{
    using System;
    using System.Collections.Generic;

    public class Person
    {
        public DateTime? BirthDate { get; set; }
        public string SSN { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Gender? Gender { get; set; }
        public Race? Race { get; set; }
        public int Age { get; set; }
        public Person Spouse { get; set; }
        public Person Dad { get; set; }
        public Person Mom { get; set; }
        public Person[] Children { get; set; }
        public List<string> Hobbies { get; set; }
        public string[] Titles { get; set; }
    }

    public enum Gender
    {
        Male,
        Female
    }

    public enum Race
    {
        White,
        Black,
        Asian,
        Hispanic,
        NativeIndian
    }

    public static class PersonExtension
    {
        public static bool IsPresident(this Person person)
        {
            return person.LastName == "Trump" && person.FirstName == "Donald";
        }

        public static bool IsAdult(this Person person, int ageThreshold)
        {
            return person.Age >= ageThreshold;
        }
    }
}