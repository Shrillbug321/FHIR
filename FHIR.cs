using System.Globalization;
using Newtonsoft.Json;

namespace FHIR;

public class FHIR
{
	static FHIR()
	{
		CultureInfo ci = new CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = ci;
		Thread.CurrentThread.CurrentUICulture = ci;
	}
	public enum RangeState
	{
		Low,Normal,High
	}
	private static Dictionary<string, string> descriptions = new()
	{
		["EKG"] = "Elektrokardiografia",
		["HRV"] = "Zmienność rytmu serca",
		["GSR"] = "Reakcja skórno-galwaniczna",
		["BPM"] = "Puls serca",
		["RESP_BELT"] = "",
		["RESP_TEMP"] = "Temperatura",
		["BVP"] = "Stymulacja biwentrykularna",
		["EMG"] = "Elektromiografia - badanie mięśni"
	};
	
	private static Dictionary<string, string> signalsLoincCodes = new()
	{
		["BPM"] = "8867-4",
		["GSR"] = "8867-4",
		["RESP_BELT"] = "8867-4",
		["RESP_TEMP"] = "8867-4",
		["BVP"] = "8867-4",
		["EMG"] = "60852-1",
		["EKG"] = "60852-1",
		["HRV"] = "60852-1",
	};
	
	private static Dictionary<string, string> unitsStandardize = new()
	{
		[" V"] = "V",
		[" S"] = "S",
		["\u00b0C"] = "Cel",
		["Ω"] = "Ohm",
	};
	
	private static Dictionary<string, string> rangeStates = new()
	{
		{"Low","Poniżej normy"},
		{"Normal","W normie"},
		{"High","Powyżej normy"}
	};
	
	private static Dictionary<string, string> rangeStatesCodes = new()
	{
		{"Low","L"},
		{"Normal","N"},
		{"High","H"}
	};

	private static Dictionary<string, int> signalTypes = new()
	{
		["BPM"] = 0,
		["GSR"] = 1,
		["RESP_BELT"] = 2,
		["RESP_TEMP"] = 3,
		["BVP"] = 4,
		["EMG"] = 5,
		["EKG"] = 10,
		["HRV"] = 11
	};
	
	private static Dictionary<string, string> bodyPartsForSignal = new()
	{
		["BPM"] = "Right arm",
		["GSR"] = "Right arm",
		["RESP_BELT"] = "Right arm",
		["RESP_TEMP"] = "Right arm",
		["BVP"] = "Right arm",
		["EMG"] = "Right arm",
		["EKG"] = "Right arm",
		["HRV"] = "Right arm",
	};
	
	private static Dictionary<string, int> bodyPartsForSignalCodes = new()
	{
		["BPM"] = 368209003,
		["GSR"] = 368209003,
		["RESP_BELT"] = 368209003,
		["RESP_TEMP"] = 368209003,
		["BVP"] = 368209003,
		["EMG"] = 368209003,
		["EKG"] = 368209003,
		["HRV"] = 368209003,
	};
	
	public record Observation
	{
		public int TypeId;
		public string Signal;
		public double Value;
		public DateTime Date;
		public string Unit;
		public string State;
	}
	public static string CreateObservation(string signal, double value, DateTime date, string unit, string status)
	{
		return $$"""
		       {
		         "resourceType" : "Observation",
		         "id" : "{{signalTypes.GetValueOrDefault(signal, -1)}}",
		         "meta" : {
		           "profile" : ["http://hl7.org/fhir/StructureDefinition/vitalsigns"]
		         },
		         "text" : {
		           "status" : "generated",
		           "div" : "{{signal}}={{value}}"
		         },
		         "status" : "final",
		         "category" : [{
		           "coding" : [{
		             "system" : "http://terminology.hl7.org/CodeSystem/observation-category",
		             "code" : "vital-signs",
		             "display" : "Sygnały życiowe"
		           }]
		         }],
		         "code" : {
		           "coding" : [{
		             "system" : "http://loinc.org",
		             "code" : "{{signalsLoincCodes[signal]}}",
		             "display" : "{{descriptions[signal]}}"
		           }],
		           "text" : "{{descriptions[signal]}}"
		         },
		         "subject" : {
		           "reference" : "Patient/example"
		         },
		         "effectiveDateTime" : "{{date.Ticks}}",
		         "performer" : [{
		           "reference" : "Practitioner/example"
		         }],
		         "interpretation" : [{
		           "coding" : [{
		             "system" : "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
		             "code" : "{{rangeStatesCodes[status]}}",
		             "display" : "{{rangeStates[status]}}"
		           }],
		           "text" : "{{rangeStates[status]}}"
		         }],
		         "bodySite" : {
		           "coding" : [{
		             "system" : "http://snomed.info/sct",
		             "code" : "{{bodyPartsForSignalCodes[signal]}}",
		             "display" : "{{bodyPartsForSignal[signal]}}"
		           }]
		         },
		         "component" : {
		           "valueQuantity" : {
		             "value" : {{value}},
		             "unit" : "{{unitsStandardize[unit]}}",
		             "system" : "http://unitsofmeasure.org",
		             "code" : "{{unitsStandardize[unit]}}"
		           }
		         }
		       }
		       """;
	}

	public static Observation GetObservationFromText(string text)
	{
		dynamic json = JsonConvert.DeserializeObject(text);
		int typeId = int.Parse(json["id"].ToString());
		string[] text2 = json["text"]["div"].ToString().Split('=');
		string signal = text2[0];
		string value = text2[1];
		string code = json["interpretation"][0]["coding"][0]["code"].ToString();
		string rangeState = rangeStatesCodes.First(c => c.Value == code).Key;
		DateTime date = new DateTime(long.Parse(json["effectiveDateTime"].ToString()));
		string unit = json["component"]["valueQuantity"]["unit"].ToString();
		Observation observation = new()
		{
			TypeId = typeId,
			Signal = signal,
			Value = double.Parse(value),
			Date = date,
			Unit = unit,
			State = rangeState
		};
		return observation;
	}
	public static List<Observation> GetObservationsFromText(string text)
	{
		dynamic jsonArray = JsonConvert.DeserializeObject(text);
		List<Observation> observations = new();
		foreach (dynamic json in jsonArray)
		{
			int typeId = int.Parse(json["id"].ToString());
			string[] text2 = json["text"]["div"].ToString().Split('=');
            string signal = text2[0];
            string value = text2[1];
            string code = json["interpretation"][0]["coding"][0]["code"].ToString();
            string rangeState = rangeStatesCodes.First(c => c.Value == code).Key;
            DateTime date = new DateTime(long.Parse(json["effectiveDateTime"].ToString()));
            string unit = json["component"]["valueQuantity"]["unit"].ToString();
            observations.Add(new Observation
            {
	            TypeId = typeId,
            	Signal = signal,
            	Value = double.Parse(value),
            	Date = date,
            	Unit = unit,
            	State = rangeState
            });
		}
		
		return observations;
	}
}