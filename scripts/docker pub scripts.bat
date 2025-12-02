docker login

docker build -t jobscraper.web -f Jobscraper.Web/Dockerfile

docker tag jobscraper.web combi71/jobscraper.web:latest
docker push combi71/jobscraper.web:latest

docker tag jobscraper.web combi71/jobscraper.web:0.3
docker push combi71/jobscraper.web:0.3

dive jobscraper.web:latest
