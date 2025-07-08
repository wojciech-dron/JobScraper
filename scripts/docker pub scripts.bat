docker login

docker tag jobscraper.web combi71/jobscraper.web:latest
docker push combi71/jobscraper.web:latest

docker tag jobscraper.web combi71/jobscraper.web:0.2.1
docker push combi71/jobscraper.web:0.2.1