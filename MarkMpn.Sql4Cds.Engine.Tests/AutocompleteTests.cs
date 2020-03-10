﻿using FakeXrmEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MarkMpn.Sql4Cds.Engine.Tests
{
    [TestClass]
    public class AutocompleteTests
    {
        private Autocomplete _autocomplete;

        public AutocompleteTests()
        {
            var context = new XrmFakedContext();
            context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var org = context.GetOrganizationService();
            var metadata = new AttributeMetadataCache(org);
            var a = metadata["account"];
            var c = metadata["contact"];

            _autocomplete = new Autocomplete(new[] { a, c }, metadata);
        }

        [TestMethod]
        public void FromClause()
        {
            var sql = "SELECT * FROM a";
            var suggestions = _autocomplete.GetSuggestions(sql, sql.Length - 1, out var currentLength).ToList();

            Assert.AreEqual(1, currentLength);
            CollectionAssert.AreEqual(new[] { "account" }, suggestions);
        }

        [TestMethod]
        public void Join()
        {
            var sql = "SELECT * FROM account le";
            var suggestions = _autocomplete.GetSuggestions(sql, sql.Length - 1, out _).ToList();

            CollectionAssert.AreEqual(Array.Empty<string>(), suggestions);
        }

        [TestMethod]
        public void JoinClause()
        {
            var sql = "SELECT * FROM account left outer join ";
            var suggestions = _autocomplete.GetSuggestions(sql, sql.Length - 1, out _).ToList();

            CollectionAssert.AreEqual(new[] { "account", "contact" }, suggestions);
        }

        [TestMethod]
        public void OnClause()
        {
            var sql = "SELECT * FROM account a left outer join contact c on a.accountid = c.p";
            var suggestions = _autocomplete.GetSuggestions(sql, sql.Length - 1, out var currentLength).ToList();

            Assert.AreEqual(1, currentLength);
            CollectionAssert.AreEqual(new[] { "parentcustomerid" }, suggestions);
        }

        [TestMethod]
        public void UniqueAttributeName()
        {
            var sql = "SELECT * FROM account a left outer join contact c on a.accountid = c.parentcustomerid where ";
            var suggestions = _autocomplete.GetSuggestions(sql, sql.Length - 1, out _).ToList();

            CollectionAssert.AreEqual(new[] { "a", "accountid", "c", "contactid", "firstname", "lastname", "name", "parentcustomerid" }, suggestions);
        }

        [TestMethod]
        public void AllAttributesInEntity()
        {
            var sql = "SELECT * FROM account a left outer join contact c on a.accountid = c.parentcustomerid where a.";
            var suggestions = _autocomplete.GetSuggestions(sql, sql.Length - 1, out _).ToList();

            CollectionAssert.AreEqual(new[] { "accountid", "createdon", "name" }, suggestions);
        }

        [TestMethod]
        public void SelectClause()
        {
            var sql = "SELECT a. FROM account a left outer join contact c on a.accountid = c.parentcustomerid";
            var suggestions = _autocomplete.GetSuggestions(sql, sql.IndexOf("."), out _).ToList();

            CollectionAssert.AreEqual(new[] { "accountid", "createdon", "name" }, suggestions);
        }

        [TestMethod]
        public void MultipleQueries()
        {
            var sql1 = "SELECT * FROM account a left outer join contact c on a.accountid = c.parentcustomerid";
            var sql2 = "SELECT  FROM account left outer join contact on account.accountid = contact.parentcustomerid";
            var sql = sql1 + "\r\n" + sql2;
            var suggestions = _autocomplete.GetSuggestions(sql, sql1.Length + 2 + sql2.IndexOf(" ") + 1, out _).ToList();

            CollectionAssert.AreEqual(new[] { "account", "accountid", "contact", "contactid", "firstname", "lastname", "name", "parentcustomerid" }, suggestions);
        }
    }
}
