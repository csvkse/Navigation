## 容器调试命令

```
# 1. 停止并移除旧容器
docker stop navigation
docker rm navigation

# 2. 使用数据卷重新部署
docker run -d \
  --name navigation \
  -p 8080:8080 \
  -v navigation-data:/app/data \
  ghcr.io/csvkse/navigation:sha-856eef9

# 3. 查看日志
docker logs navigation

# 4. 进入容器
docker exec -it navigation /bin/bash

# 5. 停止并移除容器
docker stop navigation
docker rm navigation

# 6. 查看数据卷
docker volume ls
docker volume inspect navigation-data

# 7. 删除数据卷
docker volume rm navigation-data

# 8. 删除镜像
docker rmi ghcr.io/csvkse/navigation:sha-856eef9
```
