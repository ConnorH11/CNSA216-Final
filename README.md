# CNSA-216-Final
CNSA-216 Final
Foundations
We are building an interactive web application. You may choose to make any application, but we’ve got the NRC data just hanging out and ready, right? If you choose to go your own way in terms of data, please note the following requirements:

You must have at least 1000 rows of primary data
You must paginate access to that data in pages of 50-100 elements
These requirements are covered in the NRC data due to the number of incidents.

If you want to go your own way and you need a large data set, refer to data.govLinks to an external site. for ideas, or generate your own data.

Web application requirements
The project will be a DotNet Core MVC application
The primary data can be listed via pagination.
Search can be performed to find primary data records
New records can be added to the system
The project will use EntityFramework to handle database access
Visualization
At least one aspect of the data shall be graphed using a JavaScript graphing library.
Plotly is an excellent choice
D3.js is a powerful choice (plotly is built on d3)
Security
Users can register and log in to your application
Passwords are hashed in the database
Profile photos can be uploaded and saved to Object Storage (Minio)
CI/CD
The code will be built automatically when checked into gitlab.cnsalab.net
The build process will generate a docker image of your application
The docker image will be stored in the lab docker registry
The application will be deployed automatically to 2 or more systems
Deployment targets may be containers or linux virtual machines
Infrastructure
The application shall be accessible through a web application proxy server
haproxy
traefik
nginx
Kubernetes ingress controller
Virtual machines can be provisioned on your behalf or you can be enabled to provision your own infrastructure.

Copies of the NRC database can be made available. We will be using PostgreSQL in this class.

Software Development Lifecycle
Each team shall maintain a repository in gitlab.cnsalab.net
Each team shall use the issue tracker to track action items for the project
