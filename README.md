# Easyweb.site plain example template

Simple and plain .NET 8 starting template for creating content using Easyweb CMS and ew-tag-pack.

1. If you haven't created an Easyweb account yet, sign up at https://app.easyweb.se/account/createaccount

2. If you haven't created a site to fetch content from yet, once signed in, create a new site @ My account -> My sites -> New site (plus sign)

3. Once you have an account and a site, enable your API-key by clicking the cloud in the top bar and creating a new API-key. Copy the OAuth2 API-credentals to appSettings.json -> ApiSettings where the values are marked with x's. Replace DataOptions/UnionId/"xxxx" with your 4 digit union-/site id (seen in the end of your endpointRoot, ``/extapi/[here]``). 

4. Run the app to make sure your connection is up running and working

5. Start building using the studio in Easyweb and the tag helpers provided by the Easyweb.site-packages, such as:

``<ew-template for-key="[key]" />``

``<div ew-list="[key]">More content...</div>``

``<span ew-for="[key]"></span>``

6. Read and learn more at https://www.easyweb.site

## Quick start
The Easyweb.site-framework is quite vast and has built in ways to handle most task required by a modern website. Below is a quick guide on how to build and print content from the Easyweb CMS to HTML.

### View structure
The view structure is read from the module-structure within Easyweb, and views will be searched for depending on the incoming route and set up modules, combined with some shared folders for shared content.

#### Examples of basic folder structure:

Any view added to a module in Easyweb will be searched for using its key. For example, the standard view in a module "News" will be searched for in ``/Views/News/Index.cshtml`` as well as ``/Views/News/News.cshtml`` (Both are searched for as preferences differ.) A Module-view in a "Products"-module will be searched for in ``/Views/Products/Module.cshtml`` and a Folder view similarly with ``Folder.cshtml``.

If not found, the search looks through ``/Views/Shared/[key].cshtml`` and finally ``/Views/[key].cshtml``. Having a root fallback view ``/Views/Index.cshtml`` is good practice as it would always be found as a last resort and print any default content written in it.

Apart from ``Modules`` matching folders and ``Views`` matching ``[viewKey].cshtml``, added sections within a view, or any componenent/template added to it really, will be rendered using any ``[key].cshtml`` or ``_[key].cshtml`` matching it's key when rendereding using ``<ew-template />``.

### Printing content
Once a view is resolved, the Easyweb.site has a variety of taghelpers to simplify fetching and printing content using the (custom) keys set to added templates/components in the Easyweb studio.

The most fundamental tags are:

##### <ew-template />
``<ew-template />``, or when used with key, ``<ew-template for-key="[key]" />`` will auto-render any sub-component beneath it, or if used with key, the subcomponent with the specified key.
When rendering it will search for a view matching the key or fall back to printing default content.

##### <ew-list />
``<ew-list />``, or when used with key, ``<ew-list for-key="[key]" />``, or when used as an attribute on a HTML-element, ``<div ew-list="[myKey]"></div>`` will render the inner markup for each added content in Easyweb. Works both with components with a limit set to > 1 as well as custom feature lists like `ViewListComponent``.

##### <ew-for />
``<TAG ew-for="[key]" />``, is a the final "printing" tag, which will print the content matching the key given from easyweb into the tag where it applies. 

###### Variants of ew-for for attributes
Alternatives of ew-for is ofter used for image sources and anchor hrefs, where they are used like ```<img ew-for-src="[key]" />`` and ``<a ew-for-href="[key]" />``
Above helping tags will not only set src and href, but any other default recommended content required, such as title, alt or any other attribute that applies to where it's used.
