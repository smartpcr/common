Feature: Deserialize xml into objects

Scenario: deserialize simple xml into c# objects
  Given input xml file "Schema\\V1\\V1.xml"
  When I instantiate from xml file
  Then the object should be deserialized successfully