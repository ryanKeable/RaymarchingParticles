
using System;
using System.Collections.Generic;

public static class Emoji
{
    //NOTE:
    //This dictionary class was adapted from the Emoji.cs class in this github project:
    //https://github.com/catcher-in-the-try/Full-Emoji-List

    public static string Get(string theKey)
    {
        string theEmoji = string.Empty;
        if (!string.IsNullOrEmpty(theKey) && emojiMap.TryGetValue(theKey, out theEmoji)) {
            return theEmoji;
        } else {
            return theKey;
        }
    }

    static readonly Dictionary<string, string> emojiMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        {":Copyright:",  "©"},
        {":Registered:",  "®"},
        {":Bangbang:",  "‼"},
        {":Interrobang:",  "⁉"},
        {":Tm:",  "™"},
        {":Information_Source:",  "ℹ"},
        {":Left_Right_Arrow:",  "↔"},
        {":Arrow_Up_Down:",  "↕"},
        {":Arrow_Upper_Left:",  "↖"},
        {":Arrow_Upper_Right:",  "↗"},
        {":Arrow_Lower_Right:",  "↘"},
        {":Arrow_Lower_Left:",  "↙"},
        {":Leftwards_Arrow_With_Hook:",  "↩"},
        {":Arrow_Right_Hook:",  "↪"},
        {":Watch:",  "⌚"},
        {":Hourglass:",  "⌛"},
        {":Fast_Forward:",  "⏩"},
        {":Rewind:",  "⏪"},
        {":Arrow_Double_Up:",  "⏫"},
        {":Arrow_Double_Down:",  "⏬"},
        {":Alarm_Clock:",  "⏰"},
        {":Hourglass_Flowing_Sand:",  "⏳"},
        {":M:",  "Ⓜ"},
        {":Black_Small_Square:",  "▪"},
        {":White_Small_Square:",  "▫"},
        {":Arrow_Forward:",  "▶"},
        {":Arrow_Backward:",  "◀"},
        {":White_Medium_Square:",  "◻"},
        {":Black_Medium_Square:",  "◼"},
        {":White_Medium_Small_Square:",  "◽"},
        {":Black_Medium_Small_Square:",  "◾"},
        {":Sunny:",  "☀"},
        {":Cloud:",  "☁"},
        {":Telephone:",  "☎"},
        {":Ballot_Box_With_Check:",  "☑"},
        {":Umbrella:",  "☔"},
        {":Coffee:",  "☕"},
        {":Point_Up:",  "☝"},
        {":Relaxed:",  "☺"},
        {":Aries:",  "♈"},
        {":Taurus:",  "♉"},
        {":Gemini:",  "♊"},
        {":Cancer:",  "♋"},
        {":Leo:",  "♌"},
        {":Virgo:",  "♍"},
        {":Libra:",  "♎"},
        {":Scorpius:",  "♏"},
        {":Sagittarius:",  "♐"},
        {":Capricorn:",  "♑"},
        {":Aquarius:",  "♒"},
        {":Pisces:",  "♓"},
        {":Spades:",  "♠"},
        {":Clubs:",  "♣"},
        {":Hearts:",  "♥"},
        {":Diamonds:",  "♦"},
        {":Hotsprings:",  "♨"},
        {":Recycle:",  "♻"},
        {":Wheelchair:",  "♿"},
        {":Anchor:",  "⚓"},
        {":Warning:",  "⚠"},
        {":Zap:",  "⚡"},
        {":White_Circle:",  "⚪"},
        {":Black_Circle:",  "⚫"},
        {":Soccer:",  "⚽"},
        {":Baseball:",  "⚾"},
        {":Snowman:",  "⛄"},
        {":Partly_Sunny:",  "⛅"},
        {":Ophiuchus:",  "⛎"},
        {":No_Entry:",  "⛔"},
        {":Church:",  "⛪"},
        {":Fountain:",  "⛲"},
        {":Golf:",  "⛳"},
        {":Sailboat:",  "⛵"},
        {":Tent:",  "⛺"},
        {":Fuelpump:",  "⛽"},
        {":Scissors:",  "✂"},
        {":White_Check_Mark:",  "✅"},
        {":Airplane:",  "✈"},
        {":Envelope:",  "✉"},
        {":Fist:",  "✊"},
        {":Raised_Hand:",  "✋"},
        {":V:",  "✌"},
        {":Pencil2:",  "✏"},
        {":Black_Nib:",  "✒"},
        {":Heavy_Check_Mark:",  "✔"},
        {":Heavy_Multiplication_X:",  "✖"},
        {":Sparkles:",  "✨"},
        {":Eight_Spoked_Asterisk:",  "✳"},
        {":Eight_Pointed_Black_Star:",  "✴"},
        {":Snowflake:",  "❄"},
        {":Sparkle:",  "❇"},
        {":X:",  "❌"},
        {":Negative_Squared_Cross_Mark:",  "❎"},
        {":Question:",  "❓"},
        {":Grey_Question:",  "❔"},
        {":Grey_Exclamation:",  "❕"},
        {":Exclamation:",  "❗"},
        {":Heart:",  "❤"},
        {":Heavy_Plus_Sign:",  "➕"},
        {":Heavy_Minus_Sign:",  "➖"},
        {":Heavy_Division_Sign:",  "➗"},
        {":Arrow_Right:",  "➡"},
        {":Curly_Loop:",  "➰"},
        {":Arrow_Heading_Up:",  "⤴"},
        {":Arrow_Heading_Down:",  "⤵"},
        {":Arrow_Left:",  "⬅"},
        {":Arrow_Up:",  "⬆"},
        {":Arrow_Down:",  "⬇"},
        {":Black_Large_Square:",  "⬛"},
        {":White_Large_Square:",  "⬜"},
        {":Star:",  "⭐"},
        {":O:",  "⭕"},
        {":Wavy_Dash:",  "〰"},
        {":Part_Alternation_Mark:",  "〽"},
        {":Congratulations:",  "㊗"},
        {":Secret:",  "㊙"},
        {":Mahjong:",  "🀄"},
        {":Black_Joker:",  "🃏"},
        {":A:",  "🅰"},
        {":B:",  "🅱"},
        {":O2:",  "🅾"},
        {":Parking:",  "🅿"},
        {":Ab:",  "🆎"},
        {":Cl:",  "🆑"},
        {":Cool:",  "🆒"},
        {":Free:",  "🆓"},
        {":Id:",  "🆔"},
        {":New:",  "🆕"},
        {":Ng:",  "🆖"},
        {":Ok:",  "🆗"},
        {":Sos:",  "🆘"},
        {":Up:",  "🆙"},
        {":Vs:",  "🆚"},
        {":Cn:",  "🇨 🇳"},
        {":De:",  "🇩 🇪"},
        {":Es:",  "🇪 🇸"},
        {":Fr:",  "🇫 🇷"},
        {":Uk:",  "🇬 🇧"},
        {":It:",  "🇮 🇹"},
        {":Jp:",  "🇯 🇵"},
        {":Kr:",  "🇰 🇷"},
        {":Ru:",  "🇷 🇺"},
        {":Us:",  "🇺 🇸"},
        {":Koko:",  "🈁"},
        {":Sa:",  "🈂"},
        {":U7121:",  "🈚"},
        {":U6307:",  "🈯"},
        {":U7981:",  "🈲"},
        {":U7A7A:",  "🈳"},
        {":U5408:",  "🈴"},
        {":U6E80:",  "🈵"},
        {":U6709:",  "🈶"},
        {":U6708:",  "🈷"},
        {":U7533:",  "🈸"},
        {":U5272:",  "🈹"},
        {":U55B6:",  "🈺"},
        {":Ideograph_Advantage:",  "🉐"},
        {":Accept:",  "🉑"},
        {":Cyclone:",  "🌀"},
        {":Foggy:",  "🌁"},
        {":Closed_Umbrella:",  "🌂"},
        {":Night_With_Stars:",  "🌃"},
        {":Sunrise_Over_Mountains:",  "🌄"},
        {":Sunrise:",  "🌅"},
        {":City_Sunset:",  "🌆"},
        {":City_Sunrise:",  "🌇"},
        {":Rainbow:",  "🌈"},
        {":Bridge_At_Night:",  "🌉"},
        {":Ocean:",  "🌊"},
        {":Volcano:",  "🌋"},
        {":Milky_Way:",  "🌌"},
        {":Earth_Asia:",  "🌏"},
        {":New_Moon:",  "🌑"},
        {":First_Quarter_Moon:",  "🌓"},
        {":Waxing_Gibbous_Moon:",  "🌔"},
        {":Full_Moon:",  "🌕"},
        {":Crescent_Moon:",  "🌙"},
        {":First_Quarter_Moon_With_Face:",  "🌛"},
        {":Star2:",  "🌟"},
        {":Stars:",  "🌠"},
        {":Chestnut:",  "🌰"},
        {":Seedling:",  "🌱"},
        {":Palm_Tree:",  "🌴"},
        {":Cactus:",  "🌵"},
        {":Tulip:",  "🌷"},
        {":Cherry_Blossom:",  "🌸"},
        {":Rose:",  "🌹"},
        {":Hibiscus:",  "🌺"},
        {":Sunflower:",  "🌻"},
        {":Blossom:",  "🌼"},
        {":Corn:",  "🌽"},
        {":Ear_Of_Rice:",  "🌾"},
        {":Herb:",  "🌿"},
        {":Four_Leaf_Clover:",  "🍀"},
        {":Maple_Leaf:",  "🍁"},
        {":Fallen_Leaf:",  "🍂"},
        {":Leaves:",  "🍃"},
        {":Mushroom:",  "🍄"},
        {":Tomato:",  "🍅"},
        {":Eggplant:",  "🍆"},
        {":Grapes:",  "🍇"},
        {":Melon:",  "🍈"},
        {":Watermelon:",  "🍉"},
        {":Tangerine:",  "🍊"},
        {":Banana:",  "🍌"},
        {":Pineapple:",  "🍍"},
        {":Apple:",  "🍎"},
        {":Green_Apple:",  "🍏"},
        {":Peach:",  "🍑"},
        {":Cherries:",  "🍒"},
        {":Strawberry:",  "🍓"},
        {":Hamburger:",  "🍔"},
        {":Pizza:",  "🍕"},
        {":Meat_On_Bone:",  "🍖"},
        {":Poultry_Leg:",  "🍗"},
        {":Rice_Cracker:",  "🍘"},
        {":Rice_Ball:",  "🍙"},
        {":Rice:",  "🍚"},
        {":Curry:",  "🍛"},
        {":Ramen:",  "🍜"},
        {":Spaghetti:",  "🍝"},
        {":Bread:",  "🍞"},
        {":Fries:",  "🍟"},
        {":Sweet_Potato:",  "🍠"},
        {":Dango:",  "🍡"},
        {":Oden:",  "🍢"},
        {":Sushi:",  "🍣"},
        {":Fried_Shrimp:",  "🍤"},
        {":Fish_Cake:",  "🍥"},
        {":Icecream:",  "🍦"},
        {":Shaved_Ice:",  "🍧"},
        {":Ice_Cream:",  "🍨"},
        {":Doughnut:",  "🍩"},
        {":Cookie:",  "🍪"},
        {":Chocolate_Bar:",  "🍫"},
        {":Candy:",  "🍬"},
        {":Lollipop:",  "🍭"},
        {":Custard:",  "🍮"},
        {":Honey_Pot:",  "🍯"},
        {":Cake:",  "🍰"},
        {":Bento:",  "🍱"},
        {":Stew:",  "🍲"},
        {":Egg:",  "🍳"},
        {":Fork_And_Knife:",  "🍴"},
        {":Tea:",  "🍵"},
        {":Sake:",  "🍶"},
        {":Wine_Glass:",  "🍷"},
        {":Cocktail:",  "🍸"},
        {":Tropical_Drink:",  "🍹"},
        {":Beer:",  "🍺"},
        {":Beers:",  "🍻"},
        {":Ribbon:",  "🎀"},
        {":Gift:",  "🎁"},
        {":Birthday:",  "🎂"},
        {":Jack_O_Lantern:",  "🎃"},
        {":Christmas_Tree:",  "🎄"},
        {":Santa:",  "🎅"},
        {":Fireworks:",  "🎆"},
        {":Sparkler:",  "🎇"},
        {":Balloon:",  "🎈"},
        {":Tada:",  "🎉"},
        {":Confetti_Ball:",  "🎊"},
        {":Tanabata_Tree:",  "🎋"},
        {":Crossed_Flags:",  "🎌"},
        {":Bamboo:",  "🎍"},
        {":Dolls:",  "🎎"},
        {":Flags:",  "🎏"},
        {":Wind_Chime:",  "🎐"},
        {":Rice_Scene:",  "🎑"},
        {":School_Satchel:",  "🎒"},
        {":Mortar_Board:",  "🎓"},
        {":Carousel_Horse:",  "🎠"},
        {":Ferris_Wheel:",  "🎡"},
        {":Roller_Coaster:",  "🎢"},
        {":Fishing_Pole_And_Fish:",  "🎣"},
        {":Microphone:",  "🎤"},
        {":Movie_Camera:",  "🎥"},
        {":Cinema:",  "🎦"},
        {":Headphones:",  "🎧"},
        {":Art:",  "🎨"},
        {":Tophat:",  "🎩"},
        {":Circus_Tent:",  "🎪"},
        {":Ticket:",  "🎫"},
        {":Clapper:",  "🎬"},
        {":Performing_Arts:",  "🎭"},
        {":Video_Game:",  "🎮"},
        {":Dart:",  "🎯"},
        {":Slot_Machine:",  "🎰"},
        {":_8Ball:",  "🎱"},
        {":Game_Die:",  "🎲"},
        {":Bowling:",  "🎳"},
        {":Flower_Playing_Cards:",  "🎴"},
        {":Musical_Note:",  "🎵"},
        {":Notes:",  "🎶"},
        {":Saxophone:",  "🎷"},
        {":Guitar:",  "🎸"},
        {":Musical_Keyboard:",  "🎹"},
        {":Trumpet:",  "🎺"},
        {":Violin:",  "🎻"},
        {":Musical_Score:",  "🎼"},
        {":Running_Shirt_With_Sash:",  "🎽"},
        {":Tennis:",  "🎾"},
        {":Ski:",  "🎿"},
        {":Basketball:",  "🏀"},
        {":Checkered_Flag:",  "🏁"},
        {":Snowboarder:",  "🏂"},
        {":Runner:",  "🏃"},
        {":Surfer:",  "🏄"},
        {":Trophy:",  "🏆"},
        {":Football:",  "🏈"},
        {":Swimmer:",  "🏊"},
        {":House:",  "🏠"},
        {":House_With_Garden:",  "🏡"},
        {":Office:",  "🏢"},
        {":Post_Office:",  "🏣"},
        {":Hospital:",  "🏥"},
        {":Bank:",  "🏦"},
        {":Atm:",  "🏧"},
        {":Hotel:",  "🏨"},
        {":Love_Hotel:",  "🏩"},
        {":Convenience_Store:",  "🏪"},
        {":School:",  "🏫"},
        {":Department_Store:",  "🏬"},
        {":Factory:",  "🏭"},
        {":Izakaya_Lantern:",  "🏮"},
        {":Japanese_Castle:",  "🏯"},
        {":European_Castle:",  "🏰"},
        {":Snail:",  "🐌"},
        {":Snake:",  "🐍"},
        {":Racehorse:",  "🐎"},
        {":Sheep:",  "🐑"},
        {":Monkey:",  "🐒"},
        {":Chicken:",  "🐔"},
        {":Boar:",  "🐗"},
        {":Elephant:",  "🐘"},
        {":Octopus:",  "🐙"},
        {":Shell:",  "🐚"},
        {":Bug:",  "🐛"},
        {":Ant:",  "🐜"},
        {":Bee:",  "🐝"},
        {":Beetle:",  "🐞"},
        {":Fish:",  "🐟"},
        {":Tropical_Fish:",  "🐠"},
        {":Blowfish:",  "🐡"},
        {":Turtle:",  "🐢"},
        {":Hatching_Chick:",  "🐣"},
        {":Baby_Chick:",  "🐤"},
        {":Hatched_Chick:",  "🐥"},
        {":Bird:",  "🐦"},
        {":Penguin:",  "🐧"},
        {":Koala:",  "🐨"},
        {":Poodle:",  "🐩"},
        {":Camel:",  "🐫"},
        {":Dolphin:",  "🐬"},
        {":Mouse:",  "🐭"},
        {":Cow:",  "🐮"},
        {":Tiger:",  "🐯"},
        {":Rabbit:",  "🐰"},
        {":Cat:",  "🐱"},
        {":Dragon_Face:",  "🐲"},
        {":Whale:",  "🐳"},
        {":Horse:",  "🐴"},
        {":Monkey_Face:",  "🐵"},
        {":Dog:",  "🐶"},
        {":Pig:",  "🐷"},
        {":Frog:",  "🐸"},
        {":Hamster:",  "🐹"},
        {":Wolf:",  "🐺"},
        {":Bear:",  "🐻"},
        {":Panda_Face:",  "🐼"},
        {":Pig_Nose:",  "🐽"},
        {":Feet:",  "🐾"},
        {":Eyes:",  "👀"},
        {":Ear:",  "👂"},
        {":Nose:",  "👃"},
        {":Lips:",  "👄"},
        {":Tongue:",  "👅"},
        {":Point_Up_2:",  "👆"},
        {":Point_Down:",  "👇"},
        {":Point_Left:",  "👈"},
        {":Point_Right:",  "👉"},
        {":Punch:",  "👊"},
        {":Wave:",  "👋"},
        {":Ok_Hand:",  "👌"},
        {":Thumbsup:",  "👍"},
        {":Thumbsdown:",  "👎"},
        {":Clap:",  "👏"},
        {":Open_Hands:",  "👐"},
        {":Crown:",  "👑"},
        {":Womans_Hat:",  "👒"},
        {":Eyeglasses:",  "👓"},
        {":Necktie:",  "👔"},
        {":Shirt:",  "👕"},
        {":Jeans:",  "👖"},
        {":Dress:",  "👗"},
        {":Kimono:",  "👘"},
        {":Bikini:",  "👙"},
        {":Womans_Clothes:",  "👚"},
        {":Purse:",  "👛"},
        {":Handbag:",  "👜"},
        {":Pouch:",  "👝"},
        {":Mans_Shoe:",  "👞"},
        {":Athletic_Shoe:",  "👟"},
        {":High_Heel:",  "👠"},
        {":Sandal:",  "👡"},
        {":Boot:",  "👢"},
        {":Footprints:",  "👣"},
        {":Bust_In_Silhouette:",  "👤"},
        {":Boy:",  "👦"},
        {":Girl:",  "👧"},
        {":Man:",  "👨"},
        {":Woman:",  "👩"},
        {":Family:",  "👪"},
        {":Couple:",  "👫"},
        {":Cop:",  "👮"},
        {":Dancers:",  "👯"},
        {":Bride_With_Veil:",  "👰"},
        {":Person_With_Blond_Hair:",  "👱"},
        {":Man_With_Gua_Pi_Mao:",  "👲"},
        {":Man_With_Turban:",  "👳"},
        {":Older_Man:",  "👴"},
        {":Older_Woman:",  "👵"},
        {":Baby:",  "👶"},
        {":Construction_Worker:",  "👷"},
        {":Princess:",  "👸"},
        {":Japanese_Ogre:",  "👹"},
        {":Japanese_Goblin:",  "👺"},
        {":Ghost:",  "👻"},
        {":Angel:",  "👼"},
        {":Alien:",  "👽"},
        {":Space_Invader:",  "👾"},
        {":Robot_Face:",  "🤖"},
        {":Imp:",  "👿"},
        {":Skull:",  "💀"},
        {":Information_Desk_Person:",  "💁"},
        {":Guardsman:",  "💂"},
        {":Dancer:",  "💃"},
        {":Lipstick:",  "💄"},
        {":Nail_Care:",  "💅"},
        {":Massage:",  "💆"},
        {":Haircut:",  "💇"},
        {":Barber:",  "💈"},
        {":Syringe:",  "💉"},
        {":Pill:",  "💊"},
        {":Kiss:",  "💋"},
        {":Love_Letter:",  "💌"},
        {":Ring:",  "💍"},
        {":Gem:",  "💎"},
        {":Couplekiss:",  "💏"},
        {":Bouquet:",  "💐"},
        {":Couple_With_Heart:",  "💑"},
        {":Wedding:",  "💒"},
        {":Heartbeat:",  "💓"},
        {":Broken_Heart:",  "💔"},
        {":Two_Hearts:",  "💕"},
        {":Sparkling_Heart:",  "💖"},
        {":Heartpulse:",  "💗"},
        {":Cupid:",  "💘"},
        {":Blue_Heart:",  "💙"},
        {":Green_Heart:",  "💚"},
        {":Yellow_Heart:",  "💛"},
        {":Purple_Heart:",  "💜"},
        {":Gift_Heart:",  "💝"},
        {":Revolving_Hearts:",  "💞"},
        {":Heart_Decoration:",  "💟"},
        {":Diamond_Shape_With_A_Dot_Inside:",  "💠"},
        {":Bulb:",  "💡"},
        {":Anger:",  "💢"},
        {":Bomb:",  "💣"},
        {":Zzz:",  "💤"},
        {":Boom:",  "💥"},
        {":Sweat_Drops:",  "💦"},
        {":Droplet:",  "💧"},
        {":Dash:",  "💨"},
        {":Poop:",  "💩"},
        {":Muscle:",  "💪"},
        {":Dizzy:",  "💫"},
        {":Speech_Balloon:",  "💬"},
        {":White_Flower:",  "💮"},
        {":_100:",  "💯"},
        {":Moneybag:",  "💰"},
        {":Currency_Exchange:",  "💱"},
        {":Heavy_Dollar_Sign:",  "💲"},
        {":Credit_Card:",  "💳"},
        {":Yen:",  "💴"},
        {":Dollar:",  "💵"},
        {":Money_With_Wings:",  "💸"},
        {":Chart:",  "💹"},
        {":Seat:",  "💺"},
        {":Computer:",  "💻"},
        {":Briefcase:",  "💼"},
        {":Minidisc:",  "💽"},
        {":Floppy_Disk:",  "💾"},
        {":Cd:",  "💿"},
        {":Dvd:",  "📀"},
        {":File_Folder:",  "📁"},
        {":Open_File_Folder:",  "📂"},
        {":Page_With_Curl:",  "📃"},
        {":Page_Facing_Up:",  "📄"},
        {":Date:",  "📅"},
        {":Calendar:",  "📆"},
        {":Card_Index:",  "📇"},
        {":Chart_With_Upwards_Trend:",  "📈"},
        {":Chart_With_Downwards_Trend:",  "📉"},
        {":Bar_Chart:",  "📊"},
        {":Clipboard:",  "📋"},
        {":Pushpin:",  "📌"},
        {":Round_Pushpin:",  "📍"},
        {":Paperclip:",  "📎"},
        {":Straight_Ruler:",  "📏"},
        {":Triangular_Ruler:",  "📐"},
        {":Bookmark_Tabs:",  "📑"},
        {":Ledger:",  "📒"},
        {":Notebook:",  "📓"},
        {":Notebook_With_Decorative_Cover:",  "📔"},
        {":Closed_Book:",  "📕"},
        {":Book:",  "📖"},
        {":Green_Book:",  "📗"},
        {":Blue_Book:",  "📘"},
        {":Orange_Book:",  "📙"},
        {":Books:",  "📚"},
        {":Name_Badge:",  "📛"},
        {":Scroll:",  "📜"},
        {":Pencil:",  "📝"},
        {":Telephone_Receiver:",  "📞"},
        {":Pager:",  "📟"},
        {":Fax:",  "📠"},
        {":Satellite:",  "📡"},
        {":Loudspeaker:",  "📢"},
        {":Mega:",  "📣"},
        {":Outbox_Tray:",  "📤"},
        {":Inbox_Tray:",  "📥"},
        {":Package:",  "📦"},
        {":E_Mail:",  "📧"},
        {":Incoming_Envelope:",  "📨"},
        {":Envelope_With_Arrow:",  "📩"},
        {":Mailbox_Closed:",  "📪"},
        {":Mailbox:",  "📫"},
        {":Postbox:",  "📮"},
        {":Newspaper:",  "📰"},
        {":Iphone:",  "📱"},
        {":Calling:",  "📲"},
        {":Vibration_Mode:",  "📳"},
        {":Mobile_Phone_Off:",  "📴"},
        {":Signal_Strength:",  "📶"},
        {":Camera:",  "📷"},
        {":Video_Camera:",  "📹"},
        {":Tv:",  "📺"},
        {":Radio:",  "📻"},
        {":Vhs:",  "📼"},
        {":Arrows_Clockwise:",  "🔃"},
        {":Loud_Sound:",  "🔊"},
        {":Battery:",  "🔋"},
        {":Electric_Plug:",  "🔌"},
        {":Mag:",  "🔍"},
        {":Mag_Right:",  "🔎"},
        {":Lock_With_Ink_Pen:",  "🔏"},
        {":Closed_Lock_With_Key:",  "🔐"},
        {":Key:",  "🔑"},
        {":Lock:",  "🔒"},
        {":Unlock:",  "🔓"},
        {":Bell:",  "🔔"},
        {":Bookmark:",  "🔖"},
        {":Link:",  "🔗"},
        {":Radio_Button:",  "🔘"},
        {":Back:",  "🔙"},
        {":End:",  "🔚"},
        {":On:",  "🔛"},
        {":Soon:",  "🔜"},
        {":Top:",  "🔝"},
        {":Underage:",  "🔞"},
        {":Keycap_Ten:",  "🔟"},
        {":Capital_Abcd:",  "🔠"},
        {":Abcd:",  "🔡"},
        {":_1234:",  "🔢"},
        {":Symbols:",  "🔣"},
        {":Abc:",  "🔤"},
        {":Fire:",  "🔥"},
        {":Flashlight:",  "🔦"},
        {":Wrench:",  "🔧"},
        {":Hammer:",  "🔨"},
        {":Nut_And_Bolt:",  "🔩"},
        {":Knife:",  "🔪"},
        {":Gun:",  "🔫"},
        {":Crystal_Ball:",  "🔮"},
        {":Six_Pointed_Star:",  "🔯"},
        {":Beginner:",  "🔰"},
        {":Trident:",  "🔱"},
        {":Black_Square_Button:",  "🔲"},
        {":White_Square_Button:",  "🔳"},
        {":Red_Circle:",  "🔴"},
        {":Large_Blue_Circle:",  "🔵"},
        {":Large_Orange_Diamond:",  "🔶"},
        {":Large_Blue_Diamond:",  "🔷"},
        {":Small_Orange_Diamond:",  "🔸"},
        {":Small_Blue_Diamond:",  "🔹"},
        {":Small_Red_Triangle:",  "🔺"},
        {":Small_Red_Triangle_Down:",  "🔻"},
        {":Arrow_Up_Small:",  "🔼"},
        {":Arrow_Down_Small:",  "🔽"},
        {":Clock1:",  "🕐"},
        {":Clock2:",  "🕑"},
        {":Clock3:",  "🕒"},
        {":Clock4:",  "🕓"},
        {":Clock5:",  "🕔"},
        {":Clock6:",  "🕕"},
        {":Clock7:",  "🕖"},
        {":Clock8:",  "🕗"},
        {":Clock9:",  "🕘"},
        {":Clock10:",  "🕙"},
        {":Clock11:",  "🕚"},
        {":Clock12:",  "🕛"},
        {":Mount_Fuji:",  "🗻"},
        {":Tokyo_Tower:",  "🗼"},
        {":Statue_Of_Liberty:",  "🗽"},
        {":Japan:",  "🗾"},
        {":Moyai:",  "🗿"},
        {":Grin:",  "😁"},
        {":Joy:",  "😂"},
        {":Smiley:",  "😃"},
        {":Smile:",  "😄"},
        {":Sweat_Smile:",  "😅"},
        {":Laughing:",  "😆"},
        {":Wink:",  "😉"},
        {":Blush:",  "😊"},
        {":Yum:",  "😋"},
        {":Relieved:",  "😌"},
        {":Heart_Eyes:",  "😍"},
        {":Smirk:",  "😏"},
        {":Unamused:",  "😒"},
        {":Sweat:",  "😓"},
        {":Pensive:",  "😔"},
        {":Confounded:",  "😖"},
        {":Kissing_Heart:",  "😘"},
        {":Kissing_Closed_Eyes:",  "😚"},
        {":Stuck_Out_Tongue_Winking_Eye:",  "😜"},
        {":Stuck_Out_Tongue_Closed_Eyes:",  "😝"},
        {":Disappointed:",  "😞"},
        {":Angry:",  "😠"},
        {":Rage:",  "😡"},
        {":Cry:",  "😢"},
        {":Persevere:",  "😣"},
        {":Triumph:",  "😤"},
        {":Disappointed_Relieved:",  "😥"},
        {":Fearful:",  "😨"},
        {":Weary:",  "😩"},
        {":Sleepy:",  "😪"},
        {":Tired_Face:",  "😫"},
        {":Sob:",  "😭"},
        {":Cold_Sweat:",  "😰"},
        {":Scream:",  "😱"},
        {":Astonished:",  "😲"},
        {":Flushed:",  "😳"},
        {":Dizzy_Face:",  "😵"},
        {":Mask:",  "😷"},
        {":Smile_Cat:",  "😸"},
        {":Joy_Cat:",  "😹"},
        {":Smiley_Cat:",  "😺"},
        {":Heart_Eyes_Cat:",  "😻"},
        {":Smirk_Cat:",  "😼"},
        {":Kissing_Cat:",  "😽"},
        {":Pouting_Cat:",  "😾"},
        {":Crying_Cat_Face:",  "😿"},
        {":Scream_Cat:",  "🙀"},
        {":No_Good:",  "🙅"},
        {":Ok_Woman:",  "🙆"},
        {":Bow:",  "🙇"},
        {":See_No_Evil:",  "🙈"},
        {":Hear_No_Evil:",  "🙉"},
        {":Speak_No_Evil:",  "🙊"},
        {":Raising_Hand:",  "🙋"},
        {":Raised_Hands:",  "🙌"},
        {":Person_Frowning:",  "🙍"},
        {":Person_With_Pouting_Face:",  "🙎"},
        {":Pray:",  "🙏"},
        {":Rocket:",  "🚀"},
        {":Railway_Car:",  "🚃"},
        {":Bullettrain_Side:",  "🚄"},
        {":Bullettrain_Front:",  "🚅"},
        {":Metro:",  "🚇"},
        {":Station:",  "🚉"},
        {":Bus:",  "🚌"},
        {":Busstop:",  "🚏"},
        {":Ambulance:",  "🚑"},
        {":Fire_Engine:",  "🚒"},
        {":Police_Car:",  "🚓"},
        {":Taxi:",  "🚕"},
        {":Red_Car:",  "🚗"},
        {":Blue_Car:",  "🚙"},
        {":Truck:",  "🚚"},
        {":Ship:",  "🚢"},
        {":Speedboat:",  "🚤"},
        {":Traffic_Light:",  "🚥"},
        {":Construction:",  "🚧"},
        {":Rotating_Light:",  "🚨"},
        {":Triangular_Flag_On_Post:",  "🚩"},
        {":Door:",  "🚪"},
        {":No_Entry_Sign:",  "🚫"},
        {":Smoking:",  "🚬"},
        {":No_Smoking:",  "🚭"},
        {":Bike:",  "🚲"},
        {":Walking:",  "🚶"},
        {":Mens:",  "🚹"},
        {":Womens:",  "🚺"},
        {":Restroom:",  "🚻"},
        {":Baby_Symbol:",  "🚼"},
        {":Toilet:",  "🚽"},
        {":Wc:",  "🚾"},
        {":Bath:",  "🛀"},
        {":Articulated_Lorry:",  "🚛"},
        {":Kissing_Smiling_Eyes:",  "😙"},
        {":Pear:",  "🍐"},
        {":Bicyclist:",  "🚴"},
        {":Rabbit2:",  "🐇"},
        {":Clock830:",  "🕣"},
        {":Train:",  "🚋"},
        {":Oncoming_Automobile:",  "🚘"},
        {":Expressionless:",  "😑"},
        {":Smiling_Imp:",  "😈"},
        {":Frowning:",  "😦"},
        {":No_Mouth:",  "😶"},
        {":Baby_Bottle:",  "🍼"},
        {":Non_Potable_Water:",  "🚱"},
        {":Open_Mouth:",  "😮"},
        {":Last_Quarter_Moon_With_Face:",  "🌜"},
        {":Do_Not_Litter:",  "🚯"},
        {":Sunglasses:",  "😎"},
        {":Loop:",  "➿"},
        {":Last_Quarter_Moon:",  "🌗"},
        {":Grinning:",  "😀"},
        {":Euro:",  "💶"},
        {":Clock330:",  "🕞"},
        {":Telescope:",  "🔭"},
        {":Globe_With_Meridians:",  "🌐"},
        {":Postal_Horn:",  "📯"},
        {":Stuck_Out_Tongue:",  "😛"},
        {":Clock1030:",  "🕥"},
        {":Pound:",  "💷"},
        {":Two_Men_Holding_Hands:",  "👬"},
        {":Tiger2:",  "🐅"},
        {":Anguished:",  "😧"},
        {":Vertical_Traffic_Light:",  "🚦"},
        {":Confused:",  "😕"},
        {":Repeat:",  "🔁"},
        {":Oncoming_Police_Car:",  "🚔"},
        {":Tram:",  "🚊"},
        {":Dragon:",  "🐉"},
        {":Earth_Americas:",  "🌎"},
        {":Rugby_Football:",  "🏉"},
        {":Left_Luggage:",  "🛅"},
        {":Sound:",  "🔉"},
        {":Clock630:",  "🕡"},
        {":Dromedary_Camel:",  "🐪"},
        {":Oncoming_Bus:",  "🚍"},
        {":Horse_Racing:",  "🏇"},
        {":Rooster:",  "🐓"},
        {":Rowboat:",  "🚣"},
        {":Customs:",  "🛃"},
        {":Repeat_One:",  "🔂"},
        {":Waxing_Crescent_Moon:",  "🌒"},
        {":Mountain_Railway:",  "🚞"},
        {":Clock930:",  "🕤"},
        {":Put_Litter_In_Its_Place:",  "🚮"},
        {":Arrows_Counterclockwise:",  "🔄"},
        {":Clock130:",  "🕜"},
        {":Goat:",  "🐐"},
        {":Pig2:",  "🐖"},
        {":Innocent:",  "😇"},
        {":No_Bicycles:",  "🚳"},
        {":Light_Rail:",  "🚈"},
        {":Whale2:",  "🐋"},
        {":Train2:",  "🚆"},
        {":Earth_Africa:",  "🌍"},
        {":Shower:",  "🚿"},
        {":Waning_Gibbous_Moon:",  "🌖"},
        {":Steam_Locomotive:",  "🚂"},
        {":Cat2:",  "🐈"},
        {":Tractor:",  "🚜"},
        {":Thought_Balloon:",  "💭"},
        {":Two_Women_Holding_Hands:",  "👭"},
        {":Full_Moon_With_Face:",  "🌝"},
        {":Mouse2:",  "🐁"},
        {":Clock430:",  "🕟"},
        {":Worried:",  "😟"},
        {":Rat:",  "🐀"},
        {":Ram:",  "🐏"},
        {":Dog2:",  "🐕"},
        {":Kissing:",  "😗"},
        {":Helicopter:",  "🚁"},
        {":Clock1130:",  "🕦"},
        {":No_Mobile_Phones:",  "📵"},
        {":European_Post_Office:",  "🏤"},
        {":Ox:",  "🐂"},
        {":Mountain_Cableway:",  "🚠"},
        {":Sleeping:",  "😴"},
        {":Cow2:",  "🐄"},
        {":Minibus:",  "🚐"},
        {":Clock730:",  "🕢"},
        {":Aerial_Tramway:",  "🚡"},
        {":Speaker:",  "🔈"},
        {":No_Bell:",  "🔕"},
        {":Mailbox_With_Mail:",  "📬"},
        {":No_Pedestrians:",  "🚷"},
        {":Microscope:",  "🔬"},
        {":Bathtub:",  "🛁"},
        {":Suspension_Railway:",  "🚟"},
        {":Crocodile:",  "🐊"},
        {":Mountain_Bicyclist:",  "🚵"},
        {":Waning_Crescent_Moon:",  "🌘"},
        {":Monorail:",  "🚝"},
        {":Children_Crossing:",  "🚸"},
        {":Clock230:",  "🕝"},
        {":Busts_In_Silhouette:",  "👥"},
        {":Mailbox_With_No_Mail:",  "📭"},
        {":Leopard:",  "🐆"},
        {":Deciduous_Tree:",  "🌳"},
        {":Oncoming_Taxi:",  "🚖"},
        {":Lemon:",  "🍋"},
        {":Mute:",  "🔇"},
        {":Baggage_Claim:",  "🛄"},
        {":Twisted_Rightwards_Arrows:",  "🔀"},
        {":Sun_With_Face:",  "🌞"},
        {":Trolleybus:",  "🚎"},
        {":Evergreen_Tree:",  "🌲"},
        {":Passport_Control:",  "🛂"},
        {":New_Moon_With_Face:",  "🌚"},
        {":Potable_Water:",  "🚰"},
        {":High_Brightness:",  "🔆"},
        {":Low_Brightness:",  "🔅"},
        {":Clock530:",  "🕠"},
        {":Hushed:",  "😯"},
        {":Grimacing:",  "😬"},
        {":Water_Buffalo:",  "🐃"},
        {":Neutral_Face:",  "😐"},
        {":Clock1230:",  "🕧"},
        // {"::",  ""},
        // {":Hash:",  "#"},
        // {":Zero:",  "0"},
        // {":One:",  "1"},
        // {":Two:",  "2"},
        // {":Three:",  "3"},
        // {":Four:",  "4"},
        // {":Five:",  "5"},
        // {":Six:",  "6"},
        // {":Seven:",  "7"},
        // {":Eight:",  "8"},
        // {":Nine:",  "9"},
    };
}


