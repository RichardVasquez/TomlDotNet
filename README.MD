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
* dynamic td;
3. Make a string with the contents of the TOML to parse.
* string s = "# Just a comment.\nnumber = 3";
3. Try to parse it, result is a boolean.
* var checkParse = TomlParser.TryParse(s, out td);
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

There's a ToString() in the resulting TomlDocument that will recreate the original TOML document _(sans comments)_, although there's no guarantee of the order of the text keys.  Arrays keep their order from the original document.

I implemented GetDunamicMemberNames(), but it's for debugging only, obviously.

Final
-----
* The TOMLParser project relies on Sprache available here at https://github.com/sprache/Sprache .
* This is proof of concept, including documentation.  I'll be cleaning both up as time goes by.