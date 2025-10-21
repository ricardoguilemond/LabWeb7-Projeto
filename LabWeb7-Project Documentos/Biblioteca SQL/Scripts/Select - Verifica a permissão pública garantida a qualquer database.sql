/* 
    Verifica a permissão pública garantida a qualquer database 
	O resultado deve ser sempre: 
	name      state_desc      permission_name
	public    GRANT           VIEW ANY DATABASE
*/
select pr.name,
       pe.state_desc,
	   pe.permission_name
from   sys.server_principals as pr
join   sys.server_permissions as pe on pr.principal_id = pe.grantee_principal_id
where permission_name = 'VIEW ANY DATABASE'
