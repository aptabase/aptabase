// import { Card } from "@features/primitives";
// import {
//   EventPropsWidget,
//   MainChartWidget,
//   TopAppBuildNumbersWidget,
//   TopAppVersionsWidget,
//   TopCountriesWidget,
//   TopEventsWidget,
//   TopOSVersionsWidget,
//   TopOperatingSystemsWidget,
//   TopRegionsWidget,
// } from "@features/widgets";
// import { TopNChart } from "../widgets/charts";

// export function EmptyDashboard() {
//   return (
//     <>
//       <MainChartWidget appId="" />
//       <div className="grid grid-cols-1 md:grid-cols-2 gap-[1px] pt-[1px] bg-accent">
//         <Card>
//             <TopNChart
//             title="Countries"
//             valueLabel="Sessions"
//             items={[]}
//             renderRow={(item) => (
//                 <span className="flex items-center space-x-2 px-2">
//                 <img src={getCountryFlagUrl(item.name)} className="h-5 w-5 shadow rounded-full" />
//                 <p>{getCountryName(item.name) || "Unknown"}</p>
//                 </span>
//             )}
//             />
//         </Card>
//         <Card>
//           <TopOperatingSystemsWidget appId="" />
//         </Card>
//         <Card>
//           <TopEventsWidget appId="" />
//         </Card>
//         <Card>
//           <TopAppVersionsWidget appId="" />
//         </Card>
//       </div>
//     </>
//   );
// }
