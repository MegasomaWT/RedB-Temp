=============**пример 1**==================
ОЖИДАЕМЫЙ РЕЗУЛЬТАТ для get_object_json(9001):
{
  "id": 9001,
  "key": null,
  "bool": null,
  "hash": null,
  "name": "John Doe Test",
  "note": null,
  "code_int": null,
  "owner_id": 1,
  "code_guid": null,
  "parent_id": null,
  "scheme_id": 9001,
  "date_begin": null,
  "properties": {
    "Age": 30,
    "Name": "John Doe",
    "Tags": [
      "developer",
      "senior",
      "fullstack"
    ],
    "Scores": [
      85,
      92,
      78
    ],
    "Address": {
      "City": "Moscow",
      "Street": "Main Street 123",
      "Details": {
        "Floor": 5,
        "Building": "Building A"
      }
    },
    "Contacts": [
      {
        "Type": "email",
        "Value": "john@example.com",
        "Verified": true
      },
      {
        "Type": "phone",
        "Value": "+7-999-123-45-67",
        "Verified": false
      }
    ]
  },
  "code_string": null,
  "date_create": "2025-08-25T20:27:53.17372",
  "date_modify": "2025-08-25T20:27:53.17372",
  "scheme_name": "TestPerson",
  "date_complete": null,
  "who_change_id": 1
}

var r2 = new RedbObject<root1>();

    public class root1
    {
        public int Age { get; set; }
        public string Name { get; set; }
        public string[] Tags { get; set; }
        public int[] Scores { get; set; }
        public Address Address { get; set; }
        public Contact[] Contacts { get; set; }
    }

    public class Address
    {
        public string City { get; set; }
        public string Street { get; set; }
        public Details Details { get; set; }
    }

    public class Details
    {
        public int Floor { get; set; }
        public string Building { get; set; }
    }

    public class Contact
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public bool Verified { get; set; }
    }


=============**пример 2**==================


ОЖИДАЕМЫЙ РЕЗУЛЬТАТ для get_object_json(1021)
{
  "id": 1021,
  "key": null,
  "bool": null,
  "hash": null,
  "name": "AnalyticsRecord Example",
  "note": null,
  "code_int": null,
  "owner_id": 1,
  "code_guid": null,
  "parent_id": null,
  "scheme_id": 1002,
  "date_begin": null,
  "properties": {
    "Tag": "пт",
    "Date": "2025-07-15T00:00:00",
    "Stock": 151,
    "Orders": 0,
    "Article": "пт 5х260",
    "TotalCart": 2,
    "AutoMetrics": {
      "id": 1019,
      "key": null,
      "bool": null,
      "hash": null,
      "name": "AutoMetrics Example",
      "note": null,
      "code_int": null,
      "owner_id": 1,
      "code_guid": null,
      "parent_id": null,
      "scheme_id": 1001,
      "date_begin": null,
      "properties": {
        "Base": 0,
        "Rate": 0,
        "Costs": 0,
        "Baskets": 0,
        "AdvertId": 0,
        "Association": 0
      },
      "code_string": null,
      "date_create": "2025-08-25T20:27:53.17372",
      "date_modify": "2025-08-25T20:27:53.17372",
      "scheme_name": "TrueSight.Models.AnalyticsMetrics",
      "date_complete": null,
      "who_change_id": 1
    },
    "AuctionMetrics": {
      "id": 1020,
      "key": null,
      "bool": null,
      "hash": null,
      "name": "AuctionMetrics Example",
      "note": null,
      "code_int": null,
      "owner_id": 1,
      "code_guid": null,
      "parent_id": null,
      "scheme_id": 1001,
      "date_begin": null,
      "properties": {
        "Base": 0,
        "Rate": 0,
        "Costs": 0,
        "Baskets": 0,
        "AdvertId": 0,
        "Association": 0
      },
      "code_string": null,
      "date_create": "2025-08-25T20:27:53.17372",
      "date_modify": "2025-08-25T20:27:53.17372",
      "scheme_name": "TrueSight.Models.AnalyticsMetrics",
      "date_complete": null,
      "who_change_id": 1
    },
    "RelatedMetrics": [
      {
        "id": 1019,
        "key": null,
        "bool": null,
        "hash": null,
        "name": "AutoMetrics Example",
        "note": null,
        "code_int": null,
        "owner_id": 1,
        "code_guid": null,
        "parent_id": null,
        "scheme_id": 1001,
        "date_begin": null,
        "properties": {
          "Base": 0,
          "Rate": 0,
          "Costs": 0,
          "Baskets": 0,
          "AdvertId": 0,
          "Association": 0
        },
        "code_string": null,
        "date_create": "2025-08-25T20:27:53.17372",
        "date_modify": "2025-08-25T20:27:53.17372",
        "scheme_name": "TrueSight.Models.AnalyticsMetrics",
        "date_complete": null,
        "who_change_id": 1
      },
      {
        "id": 1022,
        "key": null,
        "bool": null,
        "hash": null,
        "name": "AutoMetrics Example 2",
        "note": null,
        "code_int": null,
        "owner_id": 1,
        "code_guid": null,
        "parent_id": null,
        "scheme_id": 1001,
        "date_begin": null,
        "properties": {
          "Base": 75,
          "Rate": 95,
          "Costs": 1500.5,
          "Baskets": 50,
          "AdvertId": 100,
          "Association": 25
        },
        "code_string": null,
        "date_create": "2025-08-25T20:27:53.17372",
        "date_modify": "2025-08-25T20:27:53.17372",
        "scheme_name": "TrueSight.Models.AnalyticsMetrics",
        "date_complete": null,
        "who_change_id": 1
      }
    ]
  },
  "code_string": null,
  "date_create": "2025-08-25T20:27:53.17372",
  "date_modify": "2025-08-25T20:27:53.17372",
  "scheme_name": "TrueSight.Models.AnalyticsRecord",
  "date_complete": null,
  "who_change_id": 1
}

var r2 = new RedbObject<root2>();
    public class root2
    {
        public string Tag { get; set; }
        public DateTime Date { get; set; }
        public int Stock { get; set; }
        public int Orders { get; set; }
        public string Article { get; set; }
        public int TotalCart { get; set; }
        public RedbObject<Autometrics> AutoMetrics { get; set; }
        public RedbObject<Auctionmetrics> AuctionMetrics { get; set; }
        public RedbObject<Relatedmetric>[] RelatedMetrics { get; set; }
    }



    public class Autometrics
    {
        public int Base { get; set; }
        public int Rate { get; set; }
        public int Costs { get; set; }
        public int Baskets { get; set; }
        public int AdvertId { get; set; }
        public int Association { get; set; }
    }


    public class Auctionmetrics
    {
        public int Base { get; set; }
        public int Rate { get; set; }
        public int Costs { get; set; }
        public int Baskets { get; set; }
        public int AdvertId { get; set; }
        public int Association { get; set; }
    }


    public class Relatedmetric
    {
        public int Base { get; set; }
        public int Rate { get; set; }
        public float Costs { get; set; }
        public int Baskets { get; set; }
        public int AdvertId { get; set; }
        public int Association { get; set; }
    }

=============**пример 3**==================
может быть в перемешку как в 1 так в 2 (композиция полая) например root3