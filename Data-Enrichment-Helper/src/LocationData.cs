using System;
using System.Collections.Generic;
using System.Linq;

namespace Data_Enrichment_Helper;

public static class LocationData
{
    // ─────────────────────────────────────────────────────────
    //  US LOCATIONS — all 50 states + Washington, DC.
    //  Every entry is a real city/CDP, listed largest → smallest
    //  by population. Format: one "City, ST" per line.
    // ─────────────────────────────────────────────────────────
    public static List<string> GetUSLocations()
    {
        return _usRaw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 2)
            .ToList();
    }

    // ─────────────────────────────────────────────────────────
    //  CANADA LOCATIONS — all 13 provinces & territories.
    //  Listed largest → smallest by population.
    //  Format: one "City, Province" per line.
    // ─────────────────────────────────────────────────────────
    public static List<string> GetCanadaLocations()
    {
        return _caRaw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 2)
            .ToList();
    }

    // ─────────────────────────────────────────────────────────
    //  RAW USA DATA (population descending, nationwide)
    // ─────────────────────────────────────────────────────────
    private const string _usRaw = @"
New York, NY
Los Angeles, CA
Chicago, IL
Houston, TX
Phoenix, AZ
Philadelphia, PA
San Antonio, TX
San Diego, CA
Dallas, TX
San Jose, CA
Austin, TX
Jacksonville, FL
Fort Worth, TX
Columbus, OH
Indianapolis, IN
Charlotte, NC
San Francisco, CA
Seattle, WA
Denver, CO
Washington, DC
Nashville, TN
Oklahoma City, OK
El Paso, TX
Boston, MA
Portland, OR
Las Vegas, NV
Detroit, MI
Memphis, TN
Louisville, KY
Baltimore, MD
Milwaukee, WI
Albuquerque, NM
Tucson, AZ
Fresno, CA
Sacramento, CA
Kansas City, MO
Mesa, AZ
Atlanta, GA
Omaha, NE
Colorado Springs, CO
Raleigh, NC
Long Beach, CA
Virginia Beach, VA
Miami, FL
Oakland, CA
Minneapolis, MN
Tulsa, OK
Bakersfield, CA
Wichita, KS
Arlington, TX
Aurora, CO
Tampa, FL
New Orleans, LA
Cleveland, OH
Honolulu, HI
Anaheim, CA
Lexington, KY
Stockton, CA
Corpus Christi, TX
Henderson, NV
Riverside, CA
Newark, NJ
Saint Paul, MN
Santa Ana, CA
Cincinnati, OH
Irvine, CA
Orlando, FL
Pittsburgh, PA
St. Louis, MO
Greensboro, NC
Jersey City, NJ
Anchorage, AK
Lincoln, NE
Plano, TX
Durham, NC
Buffalo, NY
Chandler, AZ
Chula Vista, CA
Toledo, OH
Madison, WI
Gilbert, AZ
Reno, NV
Fort Wayne, IN
North Las Vegas, NV
St. Petersburg, FL
Lubbock, TX
Irving, TX
Laredo, TX
Winston-Salem, NC
Chesapeake, VA
Glendale, AZ
Garland, TX
Scottsdale, AZ
Arlington, VA
Norfolk, VA
Boise, ID
Fremont, CA
Spokane, WA
Santa Clarita, CA
Baton Rouge, LA
Richmond, VA
Hialeah, FL
San Bernardino, CA
Tacoma, WA
Modesto, CA
Huntsville, AL
Des Moines, IA
Yonkers, NY
Rochester, NY
Moreno Valley, CA
Fayetteville, NC
Fontana, CA
Columbus, GA
Worcester, MA
Port St. Lucie, FL
Little Rock, AR
Augusta, GA
Oxnard, CA
Birmingham, AL
Montgomery, AL
Frisco, TX
Amarillo, TX
Salt Lake City, UT
Grand Rapids, MI
Huntington Beach, CA
Akron, OH
Overland Park, KS
Glendale, CA
Tallahassee, FL
Grand Prairie, TX
McKinney, TX
Cape Coral, FL
Knoxville, TN
Sioux Falls, SD
Peoria, AZ
Providence, RI
Vancouver, WA
Shreveport, LA
Mobile, AL
Brownsville, TX
Newport News, VA
Fort Lauderdale, FL
Chattanooga, TN
Tempe, AZ
Aurora, IL
Santa Rosa, CA
Eugene, OR
Salem, OR
Cary, NC
Pembroke Pines, FL
Fort Collins, CO
Springfield, MO
Paterson, NJ
Alexandria, VA
Macon, GA
Kansas City, KS
Lakewood, CO
Springfield, MA
Jackson, MS
Killeen, TX
Hollywood, FL
Murfreesboro, TN
Pasadena, TX
Bellevue, WA
Joliet, IL
Charleston, SC
Mesquite, TX
Naperville, IL
Rockford, IL
Bridgeport, CT
Syracuse, NY
Savannah, GA
Surprise, AZ
McAllen, TX
Thornton, CO
Olathe, KS
Gainesville, FL
West Valley City, UT
Denton, TX
Warren, MI
Waco, TX
Cedar Rapids, IA
Dayton, OH
Elizabeth, NJ
Hampton, VA
Columbia, SC
Kent, WA
Stamford, CT
Lakewood, NJ
Miramar, FL
Coral Springs, FL
Sterling Heights, MI
New Haven, CT
Midland, TX
Norman, OK
Athens, GA
Topeka, KS
Columbia, MO
Fargo, ND
Allentown, PA
Arvada, CO
Ann Arbor, MI
Independence, MO
Rochester, MN
Lafayette, LA
Hartford, CT
Palm Bay, FL
Cambridge, MA
Meridian, ID
West Palm Beach, FL
Evansville, IN
Clearwater, FL
Billings, MT
West Jordan, UT
Manchester, NH
Lowell, MA
Wilmington, NC
Provo, UT
North Charleston, SC
Elgin, IL
Waterbury, CT
Springfield, IL
Gresham, OR
High Point, NC
Broken Arrow, OK
Peoria, IL
Lansing, MI
Lakeland, FL
Pompano Beach, FL
Pueblo, CO
Everett, WA
Las Cruces, NM
Dearborn, MI
Greeley, CO
Sparks, NV
Centennial, CO
Boulder, CO
Sandy Springs, GA
Edison, NJ
South Fulton, GA
Renton, WA
Hillsboro, OR
Brockton, MA
Concord, NC
Green Bay, WI
Columbia, MD
Rio Rancho, NM
Woodbridge, NJ
South Bend, IN
Spokane Valley, WA
Davenport, IA
Quincy, MA
Goodyear, AZ
Lynn, MA
Lee's Summit, MO
New Bedford, MA
Federal Way, WA
Nampa, ID
Roanoke, VA
San Tan Valley, AZ
Carmel, IN
Tuscaloosa, AL
Kenosha, WI
Albany, NY
Bend, OR
Fishers, IN
Longmont, CO
Orem, UT
Portsmouth, VA
Beaverton, OR
Yakima, WA
Sandy, UT
Yuma, AZ
Livonia, MI
Toms River, NJ
St. George, UT
Reading, PA
Lawrence, KS
Suffolk, VA
Edmond, OK
Asheville, NC
Erie, PA
Fall River, MA
Fayetteville, AR
Roswell, GA
Hoover, AL
Bellingham, WA
Kirkland, WA
Norwalk, CT
O'Fallon, MO
Buckeye, AZ
Nashua, NH
Germantown, MD
Lawton, OK
Trenton, NJ
Mount Pleasant, SC
Clifton, NJ
Bloomington, MN
Avondale, AZ
Waukegan, IL
Lawrence, MA
Fort Smith, AR
Newton, MA
Champaign, IL
Greenville, NC
Santa Fe, NM
Troy, MI
Ogden, UT
Auburn, WA
Duluth, MN
Danbury, CT
Sioux City, IA
Medford, OR
Westland, MI
Cicero, IL
Lake Charles, LA
Springdale, AR
Kennewick, WA
Farmington Hills, MI
Franklin, TN
Warwick, RI
Johns Creek, GA
Cranston, RI
Silver Spring, MD
Layton, UT
Plymouth, MN
Parma, OH
Waldorf, MD
Somerville, MA
Gastonia, NC
Warner Robins, GA
New Rochelle, NY
Bloomington, IN
Lynchburg, VA
Frederick, MD
Schaumburg, IL
Bloomington, IL
Jonesboro, AR
Evanston, IL
Racine, WI
South Jordan, UT
Pasco, WA
Wyoming, MI
Loveland, CO
St. Charles, MO
Southfield, MI
Auburn, AL
Rochester Hills, MI
Arlington Heights, IL
Missoula, MT
Redmond, WA
Scranton, PA
Woodbury, MN
Decatur, IL
Lehi, UT
Appleton, WI
Bethlehem, PA
Pawtucket, RI
Ellicott City, MD
Iowa City, IA
Rapid City, SD
New Britain, CT
Rock Hill, SC
Camden, NJ
Kalamazoo, MI
Bismarck, ND
Castle Rock, CO
Bolingbrook, IL
Centreville, VA
Gulfport, MS
Bowling Green, KY
Glen Burnie, MD
Jacksonville, NC
St. Joseph, MO
Mount Vernon, NY
Framingham, MA
Dale City, VA
Waukesha, WI
Dothan, AL
Johnson City, TN
Canton, OH
Greenville, SC
Wilmington, DE
Marysville, WA
Passaic, NJ
Blaine, MN
Lafayette, IN
Maple Grove, MN
Rogers, AR
Lakeville, MN
Gaithersburg, MD
East Orange, NJ
Noblesville, IN
Albany, GA
Eau Claire, WI
West Des Moines, IA
Bayonne, NJ
Portland, ME
Union City, NJ
St. Cloud, MN
Jackson, TN
Shawnee, KS
Palatine, IL
Skokie, IL
Eagan, MN
Haverhill, MA
Dundalk, MD
Ankeny, IA
Sammamish, WA
Waterloo, IA
Rockville, MD
Schenectady, NY
Oshkosh, WI
Malden, MA
Kenner, LA
Ames, IA
Pontiac, MI
Janesville, WI
Grand Junction, CO
Lorain, OH
Alpharetta, GA
Muncie, IN
Cheyenne, WY
Waltham, MA
Utica, NY
Eden Prairie, MN
Idaho Falls, ID
North Little Rock, AR
Burnsville, MN
West Hartford, CT
Bellevue, NE
Conway, AR
Taylor, MI
Greenwood, IN
Lakewood, WA
Coon Rapids, MN
Greenwich, CT
Millcreek, UT
Hamilton, OH
Dearborn Heights, MI
Reston, VA
Bossier City, LA
Moore, OK
Apex, NC
Council Bluffs, IA
Revere, MA
Chapel Hill, NC
Fairfield, CT
Springfield, OR
Hendersonville, TN
Huntersville, NC
Vineland, NJ
Bristol, CT
Hamden, CT
Marietta, GA
Great Falls, MT
Youngstown, OH
Taylorsville, UT
Manchester, CT
West Allis, WI
Hoboken, NJ
Owensboro, KY
Caldwell, ID
Corvallis, OR
Dubuque, IA
Albany, OR
White Plains, NY
Towson, MD
St. Clair Shores, MI
Medford, MA
Taunton, MA
Grand Forks, ND
Kokomo, IN
Springfield, OH
Carson City, NV
Casper, WY
Terre Haute, IN
Blue Springs, MO
Bowie, MD
Midwest City, OK
Lancaster, PA
Decatur, AL
Bartlett, TN
Kettering, OH
Lenexa, KS
Burlington, NC
St. Peters, MO
Smyrna, GA
Madison, AL
Apple Valley, MN
Pocatello, ID
Perth Amboy, NJ
New Brunswick, NJ
Kingsport, TN
West Haven, CT
Herriman, UT
Hempstead, NY
Anderson, IN
Plainfield, NJ
Southaven, MS
Normal, IL
Manhattan, KS
Tigard, OR
Rocky Mount, NC
Bentonville, AR
Elkhart, IN
Chillum, MD
Edina, MN
Bozeman, MT
Kannapolis, NC
Smyrna, TN
Grand Island, NE
Elyria, OH
Logan, UT
Minnetonka, MN
La Crosse, WI
Florissant, MO
Coeur d'Alene, ID
Wheaton, MD
Enid, OK
Aspen Hill, MD
Harrisonburg, VA
Joplin, MO
Twin Falls, ID
Collierville, TN
Troy, NY
Cuyahoga Falls, OH
Draper, UT
Summerville, SC
Middletown, OH
North Bethesda, MD
Murray, UT
Severn, MD
St. Louis Park, MN
Lakewood, OH
Harrisburg, PA
East Honolulu, HI
Spring Hill, TN
Chesterfield, MO
Sheboygan, WI
Euclid, OH
Newark, OH
Biloxi, MS
Hattiesburg, MS
Niagara Falls, NY
Minot, ND
Wauwatosa, WI
Charleston, WV
Roswell, NM
Leesburg, VA
Stillwater, OK
Monroe, LA
Binghamton, NY
Mentor, OH
East Providence, RI
Cleveland, TN
Farmington, NM
Salina, KS
Charlottesville, VA
Huntington, WV
Goose Creek, SC
Alexandria, LA
Bountiful, UT
Urbandale, IA
Pearl City, HI
Brentwood, TN
Altoona, PA
Blacksburg, VA
York, PA
Marion, IA
Burlington, VT
Catonsville, MD
Woodbridge, VA
Fond du Lac, WI
Riverton, UT
Gallatin, TN
Wentzville, MO
Wilkes-Barre, PA
Hilo, HI
Concord, NH
Freeport, NY
Eagle Mountain, UT
Woonsocket, RI
Sumter, SC
Hagerstown, MD
Waipahu, HI
Jefferson City, MO
State College, PA
Columbia, TN
Brookfield, WI
Germantown, TN
Covington, KY
Kailua, HI
Cedar Falls, IA
New Berlin, WI
Valley Stream, NY
Florence, AL
Hobbs, NM
Hutchinson, KS
Wausau, WI
Florence, SC
Rexburg, ID
Menomonee Falls, WI
Bettendorf, IA
Sun Prairie, WI
Cape Girardeau, MO
Olive Branch, MS
Keizer, OR
Grants Pass, OR
Dover, DE
Owasso, OK
Phenix City, AL
Spartanburg, SC
Post Falls, ID
West Fargo, ND
Clovis, NM
La Vergne, TN
Hot Springs, AR
Bartlesville, OK
Hilton Head Island, SC
Leavenworth, KS
Lewiston, ME
Tupelo, MS
Kaneohe, HI
Yukon, OK
Georgetown, KY
Cumberland, RI
Butte, MT
Norristown, PA
Coventry, RI
Myrtle Beach, SC
Benton, AR
Wildwood, MO
Meridian, MS
Greer, SC
Richmond, KY
University City, MO
Derry, NH
McMinnville, OR
Lewiston, ID
North Providence, RI
Leawood, KS
Prairieville, LA
Kearney, NE
Bethel Park, PA
Gillette, WY
Long Beach, NY
Spring Valley, NY
Houma, LA
Helena, MT
Dover, NH
Sherwood, AR
Laramie, WY
Chester, PA
Florence, KY
Fairbanks, AK
Rome, NY
Rochester, NH
Juneau, AK
Slidell, LA
Carlsbad, NM
Aiken, SC
South Kingstown, RI
Alamogordo, NM
Bangor, ME
Eagle, ID
Nicholasville, KY
Shawnee, OK
Elizabethtown, KY
West Warwick, RI
Hopkinsville, KY
Salem, NH
Newark, DE
Morgantown, WV
Liberty, MO
Hazleton, PA
Greenville, MS
Johnston, RI
Parkersburg, WV
Williston, ND
Central, LA
Kahului, HI
Bixby, OK
Independence, KY
Frankfort, KY
Jeffersontown, KY
Aberdeen, SD
Easton, PA
Mililani Town, HI
New Iberia, LA
Henderson, KY
Dodge City, KS
North Kingstown, RI
Madison, MS
Merrimack, NH
Fremont, NE
Wheeling, WV
South Portland, ME
Lebanon, PA
Pearl, MS
Horn Lake, MS
Londonderry, NH
Norfolk, NE
Oxford, MS
Dickinson, ND
Brandon, MS
Derby, KS
Moscow, ID
Starkville, MS
Hudson, NH
Newport, RI
Hastings, NE
Kalispell, MT
New Castle, PA
Columbus, NE
Kuna, ID
Mandan, ND
Papillion, NE
Auburn, ME
North Platte, NE
Bedford, NH
Brookings, SD
Keene, NH
Westerly, RI
Middletown, DE
Rock Springs, WY
Fernley, NV
Scarborough, ME
Watertown, SD
Biddeford, ME
Ruston, LA
Sanford, ME
Kihei, HI
Gallup, NM
Kapolei, HI
Elko, NV
Mesquite, NV
Saco, ME
South Burlington, VT
Westbrook, ME
Weirton, WV
Augusta, ME
Martinsburg, WV
Sheridan, WY
Fairmont, WV
Windham, ME
Wailuku, HI
Los Lunas, NM
Beckley, WV
Clarksburg, WV
La Vista, NE
Rutland, VT
Jamestown, ND
Mitchell, SD
Yankton, SD
Bennington, VT
Deming, NM
Boulder City, NV
Huron, SD
Pierre, SD
South Charleston, WV
Smyrna, DE
Artesia, NM
Spearfish, SD
Brattleboro, VT
Portales, NM
Garden City, ID
Milford, DE
Green River, WY
Evanston, WY
Vermillion, SD
Riverton, WY
Brandon, SD
Jackson, WY
Vienna, WV
Belgrade, MT
Cody, WY
Havre, MT
Middlebury, VT
Springfield, VT
Wasilla, AK
Barre, VT
Sitka, AK
Winooski, VT
Ketchikan, AK
Montpelier, VT
Seaford, DE
Whitefish, MT
Lander, WY
Kenai, AK
St. Johnsbury, VT
Georgetown, DE
St. Albans, VT
Bethel, AK
Elsmere, DE
Watford City, ND
Palmer, AK
Kodiak, AK
New Castle, DE
";

    // ─────────────────────────────────────────────────────────
    //  RAW CANADA DATA (population descending, nationwide)
    // ─────────────────────────────────────────────────────────
    private const string _caRaw = @"
Toronto, ON
Montreal, QC
Calgary, AB
Ottawa, ON
Edmonton, AB
Winnipeg, MB
Mississauga, ON
Vancouver, BC
Brampton, ON
Hamilton, ON
Surrey, BC
Quebec City, QC
Halifax, NS
Laval, QC
London, ON
Markham, ON
Vaughan, ON
Gatineau, QC
Saskatoon, SK
Kitchener, ON
Longueuil, QC
Burnaby, BC
Windsor, ON
Regina, SK
Oakville, ON
Richmond, BC
Richmond Hill, ON
Oshawa, ON
Sherbrooke, QC
Sudbury, ON
Abbotsford, BC
Lévis, QC
Coquitlam, BC
Barrie, ON
Saguenay, QC
Kelowna, BC
Guelph, ON
Trois-Rivières, QC
Whitby, ON
Cambridge, ON
St. Catharines, ON
Milton, ON
Langley, BC
Kingston, ON
Ajax, ON
Waterloo, ON
Terrebonne, QC
Saanich, BC
St. John's, NL
Thunder Bay, ON
Delta, BC
Brantford, ON
Red Deer, AB
Nanaimo, BC
Pickering, ON
Lethbridge, AB
Saint-Jean-sur-Richelieu, QC
Kamloops, BC
Niagara Falls, ON
Chilliwack, BC
Victoria, BC
Brossard, QC
Repentigny, QC
Peterborough, ON
Saint-Jérôme, QC
Moncton, NB
Drummondville, QC
Prince George, BC
Airdrie, AB
Sault Ste. Marie, ON
Saint John, NB
Granby, QC
St. Albert, AB
Fort McMurray, AB
Grande Prairie, AB
Fredericton, NB
Medicine Hat, AB
Mirabel, QC
Blainville, QC
Saint-Hyacinthe, QC
North Bay, ON
Brandon, MB
Shawinigan, QC
Timmins, ON
Charlottetown, PEI
Prince Albert, SK
Spruce Grove, AB
Leduc, AB
Moose Jaw, SK
Cochrane, AB
Lloydminster, AB
Okotoks, AB
Sydney, NS
Whitehorse, YT
Dieppe, NB
Fort Saskatchewan, AB
Conception Bay South, NL
Dartmouth, NS
Cole Harbour, NS
Bedford, NS
Mount Pearl, NL
Lower Sackville, NS
Paradise, NL
Yellowknife, NT
Riverview, NB
Corner Brook, NL
Quispamsis, NB
Steinbach, MB
Miramichi, NB
Glace Bay, NS
Swift Current, SK
Edmundston, NB
Yorkton, SK
Summerside, PEI
North Battleford, SK
Winkler, MB
Grand Falls-Windsor, NL
Sydney Mines, NS
Portage la Prairie, MB
Thompson, MB
Truro, NS
Warman, SK
Bathurst, NB
Gander, NL
Lloydminster, SK
Bay Roberts, NL
Stratford, PEI
Weyburn, SK
Estevan, SK
Martensville, SK
Selkirk, MB
Morden, MB
New Glasgow, NS
Dauphin, MB
Portugal Cove-St. Philip's, NL
Happy Valley-Goose Bay, NL
Torbay, NL
Iqaluit, NU
Campbellton, NB
Labrador City, NL
Niverville, MB
Cornwall, PEI
The Pas, MB
Flin Flon, MB
Inuvik, NT
Hay River, NT
Rankin Inlet, NU
Arviat, NU
Baker Lake, NU
Igloolik, NU
Fort Smith, NT
Montague, PEI
Cambridge Bay, NU
Behchokǫ̀, NT
Kensington, PEI
Dawson City, YT
Pond Inlet, NU
Pangnirtung, NU
Fort Simpson, NT
Tuktoyaktuk, NT
Watson Lake, YT
Norman Wells, NT
Haines Junction, YT
Carmacks, YT
Ross River, YT
Carcross, YT
Mayo, YT
";
}