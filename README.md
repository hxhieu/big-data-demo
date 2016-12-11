# big-data-demo
Hadoop vs BigQuery

# Usage
1. Have a blank SQL database, then update the connection string in the appsettings.json with it
2. Run Database-Update
3. Launch the app and create user / login
4. Create a new Authentication app after logging in
5. Get the OAuth accesstoken via client credentials with the token endpoint @ /Account/GetAuthToken

# APIs
## HDInsight
* POST /api/InsightPhoto/AddNewPhoto
* GET /api/GetTop10Photo

## BigQuery
* POST /api/InsightPhoto/AddPhotoBigQuery
* GET /api/GetTop10PhotoBigQuery

All POSTs taking the payload of { "id": INT, "title": STRING, "url": STRING }
