TomlDotNet
==========

This is a very dirty version of a TOML parser.  However, though, it works.

It uses _dynamic_ so it's only going to work in .NET 4.0 or above.

The main code is in the TOMLParser project, and some examples of the usage are in the TOML.Demo project.

Since this does use dynamic, dotted names are available as well as indexing.

Using the demo TOML file at https://github.com/mojombo/toml , you can use the following ways to get the same value:

Parsing a TOML Document
-----------------------
1. Reference the TomlParser library.
2. Create a dynamic.
   * <pre>dynamic td;</pre>
3. Make a string with the contents of the TOML to parse.
   * <pre>string s = "# Just a comment.\nnumber = 3";</pre>
3. Try to parse it, result is a boolean.
   * <pre>var checkParse = TomlParser.TryParse(s, out td);</pre>
4. If checkParse is true, it parsed.

Referencing a TOML Document
---------------------------

_(Assume your TOML document has been parsed into td, and we're using the original test file)_

* Get the last port in database.ports
  * long i = td.database.ports[2];
  * long i = td["database"]["ports"][2];
  * long i = td["database", "ports", 2];
  * long i = td["database.ports"][2];

* Retrieve a Hashtable (two types, flat and nested), then query that.
  * Flat Hashtables have all the possible keygroups listed out as keys barring the array indices.
    As above, you'd have to do this: <pre>long i = flatHash["database.ports"][2];</pre>
  * Treed Hashtables: <pre>long i = treeHash["database"]["ports"][2];</pre>

Other things
------------

There's a ToString() in the resulting TomlDocument that will recreate the original TOML document _sans comments_.  The output is sorted first with keys and then with arrays.  Each subgroup is alphabetized by name, and arrays attempt to pretty print.

I'm not that good at pretty print.

I implemented GetDynamicMemberNames(), but it's for debugging only, obviously.

TODO
----
More testing.  Right now, I do the following:

1. Read a TOML text file.
2. Parse it into a TomlDocument.
3. Extract the string version of the TomlDocument.
4. Parse the new string.
5. Extract the string version of the new TomlDocument.
6. Compare the strings.
7. Since they **should** represent the same data, and thus parse to the same results, if the strings match, then the test passes.

I've run this on both the example.toml file provided initially, and then the harder.toml version presented in the tests directory of the original TOML, and it worked swimmingly both times.

Final
-----
* The TOMLParser project relies on Sprache available here at https://github.com/sprache/Sprache .
* This is proof of concept, including documentation.  I'll be cleaning both up as time goes by.